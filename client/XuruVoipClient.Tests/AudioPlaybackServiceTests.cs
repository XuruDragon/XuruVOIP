using System;
using System.Linq;
using System.Reflection;
using Xunit;
using XuruVoipClient.Services;
using XuruVoipClient.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace XuruVoipClient.Tests;

public class AudioPlaybackServiceTests
{
    [Fact]
    public void GenerateKeyDownChime_ShouldCreateSinePitchSweep()
    {
        // GIVEN
        var method = typeof(AudioPlaybackService).GetMethod(
            "GenerateKeyDownChime",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateKeyDownChime method not found");

        // WHEN
        float[] chime = (float[])method.Invoke(null, null)!;

        // THEN: Ensure length is 50ms at 48000Hz = 2400 samples
        Assert.NotNull(chime);
        Assert.Equal(2400, chime.Length);

        // Ensure there is actual signal generated (not all zeros)
        Assert.Contains(chime, s => s != 0f);

        // Ensure samples are within quiet range (<= 15% gain as designed)
        foreach (var sample in chime)
        {
            Assert.InRange(sample, -0.16f, 0.16f);
        }
    }

    [Fact]
    public void GenerateKeyUpChime_ShouldCreateBandpassNoiseSquelch()
    {
        // GIVEN
        var method = typeof(AudioPlaybackService).GetMethod(
            "GenerateKeyUpChime",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateKeyUpChime method not found");

        // WHEN
        float[] chime = (float[])method.Invoke(null, null)!;

        // THEN: Ensure length is 180ms at 48000Hz = 8640 samples
        Assert.NotNull(chime);
        Assert.Equal(8640, chime.Length);

        // Ensure there is noise generated
        Assert.Contains(chime, s => s != 0f);

        // Ensure samples are within squelch tail gain limits (<= 25% gain)
        foreach (var sample in chime)
        {
            Assert.InRange(sample, -0.26f, 0.26f);
        }
    }

    [Fact]
    public void EnvironmentalAcousticFilter_ShouldApplyOcclusionAndReverb()
    {
        // GIVEN
        var filter = new EnvironmentalAcousticFilter();
        float[] buffer = new float[1000];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0.5f;
        }

        // WHEN: Under same zone
        filter.UpdateZoneInfo("ZoneA", "ZoneA");
        // Run multiple times to let smooth interpolation converge
        for (int i = 0; i < 50; i++)
        {
            filter.Process(buffer, buffer.Length);
        }
        // Signal should be close to original
        Assert.True(buffer[0] > 0.4f);

        // WHEN: Under different zones (Occlusion)
        filter.UpdateZoneInfo("ZoneA", "ZoneB");
        for (int i = 0; i < 100; i++)
        {
            filter.Process(buffer, buffer.Length);
        }
        // Occluded volume factor target is 0.65f, so output should be reduced
        Assert.True(buffer[0] < 0.45f);

        // WHEN: Listener is in a Cave (Reverb active)
        filter.UpdateZoneInfo("ZoneA", "Cave_System");
        for (int i = 0; i < 100; i++)
        {
            filter.Process(buffer, buffer.Length);
        }
        // Ensure samples are within safe clipping bounds
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -1.0f, 1.0f);
        }
    }

    [Fact]
    public void AudioPlaybackService_IntercomPriorityDucking_ShouldReduceProximityVolume()
    {
        // GIVEN
        var service = new AudioPlaybackService();
        var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)) { ReadFully = true };
        var mixerField = typeof(AudioPlaybackService).GetField("_mixer", BindingFlags.NonPublic | BindingFlags.Instance);
        mixerField!.SetValue(service, mixer);

        // Generate a valid silent Opus frame
        var encoder = Concentus.OpusCodecFactory.CreateEncoder(48000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
        var pcm = new short[960];
        var outBuf = new byte[4000];
        int encodedLen = encoder.Encode(pcm, pcm.Length, outBuf, outBuf.Length);
        var validOpus = new byte[encodedLen];
        Array.Copy(outBuf, validOpus, encodedLen);

        // Track 1: Pilot transmitting on intercom channel
        // Pilot is in a cockpit/seat
        var pilotOpus = validOpus;

        // Track 2: Other player in proximity speaking
        var playerOpus = validOpus;
        var metadata = new ProximityMetadata
        {
            SpeakerX = 0, SpeakerY = 0, SpeakerZ = 0,
            Distance = 10f, MaxRange = 50f,
            SpatialEnabled = false
        };

        // Enqueue 3 packets for both to fill TargetDelayFrames = 3 in JitterBuffer
        for (ushort seq = 1; seq <= 3; seq++)
        {
            service.ReceiveOpusFrame(
                playerName: "PilotJoe",
                opusData: pilotOpus,
                audioType: 0x01, // Radio (used for Intercom)
                applyRadioEffect: true,
                metadata: null,
                distance: 5.0,
                speakerZone: "Cockpit_Seat",
                listenerZone: "Cargo_Bay",
                seq: seq,
                isIntercom: true
            );

            service.ReceiveOpusFrame(
                playerName: "PlayerBob",
                opusData: playerOpus,
                audioType: 0x00, // Proximity
                applyRadioEffect: false,
                metadata: metadata,
                distance: 10.0,
                speakerZone: "Cargo_Bay",
                listenerZone: "Cargo_Bay",
                seq: seq,
                isIntercom: false
            );
        }

        // Track 3: We get the tracks via reflection
        var tracksField = typeof(AudioPlaybackService).GetField("_tracks", BindingFlags.NonPublic | BindingFlags.Instance);
        var tracks = (System.Collections.IDictionary)tracksField!.GetValue(service)!;
        
        var bobTrack = tracks["PlayerBob"]!;
        var joeTrack = tracks["PilotJoe"]!;

        var tickMethod = typeof(AudioPlaybackService).GetMethod("TickPlayback", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // WHEN
        tickMethod!.Invoke(service, null);

        // THEN
        // In Bob's Panning & Volume, Bob is proximity (audioType 0x00).
        // Since PilotJoe is transmitting on intercom and has "Cockpit" in speakerZone, pilotIntercomActive is true.
        // Therefore, Bob's track volume factor is multiplied by 0.15f (85% ducked).
        // Bob's base distance attenuation factor for 10m is 0.8f, so 0.8f * 0.15f = 0.12f.
        var volumeProp = bobTrack.GetType().GetProperty("Volume");
        var volumeProvider = (VolumeSampleProvider)volumeProp!.GetValue(bobTrack)!;
        
        Assert.Equal(0.12f, volumeProvider.Volume, 3); // 0.8f * 0.15f = 0.12f
    }

    [Fact]
    public void MegaphoneDspFilter_ShouldApplyBandpassAndClipping()
    {
        // GIVEN
        var filter = new MegaphoneDspFilter();
        // Create 1kHz sine wave
        float[] buffer = new float[960];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (float)Math.Sin(2 * Math.PI * 1000.0 * i / 48000.0);
        }

        float[] original = new float[960];
        Array.Copy(buffer, original, buffer.Length);

        // WHEN
        filter.Process(buffer, buffer.Length);

        // THEN: Audio should be modified
        bool isModified = false;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != original[i])
            {
                isModified = true;
                break;
            }
        }
        Assert.True(isModified);

        // All samples must be constrained to safe range
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -1.0f, 1.0f);
        }
    }

    [Fact]
    public void AudioPlaybackService_PaKlaxonChime_ShouldPlayChimeOnPaStart()
    {
        // GIVEN
        var service = new AudioPlaybackService();
        var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)) { ReadFully = true };
        var mixerField = typeof(AudioPlaybackService).GetField("_mixer", BindingFlags.NonPublic | BindingFlags.Instance);
        mixerField!.SetValue(service, mixer);

        // Enqueue 3 packets of type 0x03 (PA) to trigger playback loop
        var encoder = Concentus.OpusCodecFactory.CreateEncoder(48000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
        var pcm = new short[960];
        var outBuf = new byte[4000];
        int encodedLen = encoder.Encode(pcm, pcm.Length, outBuf, outBuf.Length);
        var validOpus = new byte[encodedLen];
        Array.Copy(outBuf, validOpus, encodedLen);

        for (ushort seq = 1; seq <= 3; seq++)
        {
            service.ReceiveOpusFrame(
                playerName: "CaptainBob",
                opusData: validOpus,
                audioType: 0x03, // PA
                applyRadioEffect: false,
                metadata: null,
                distance: -1.0,
                speakerZone: "Bridge",
                listenerZone: "Cargo_Bay",
                seq: seq,
                isIntercom: false
            );
        }

        var tracksField = typeof(AudioPlaybackService).GetField("_tracks", BindingFlags.NonPublic | BindingFlags.Instance);
        var tracks = (System.Collections.IDictionary)tracksField!.GetValue(service)!;

        var tickMethod = typeof(AudioPlaybackService).GetMethod("TickPlayback", BindingFlags.NonPublic | BindingFlags.Instance);

        // WHEN: Tick is invoked
        tickMethod!.Invoke(service, null);

        // THEN: CaptainBob's track should be created, transmitting, and last audio type = 0x03 (PA)
        Assert.True(tracks.Contains("CaptainBob"));
        var captainTrack = tracks["CaptainBob"]!;
        
        var isTransmittingProp = captainTrack.GetType().GetProperty("IsTransmitting");
        bool isTransmitting = (bool)isTransmittingProp!.GetValue(captainTrack)!;
        Assert.True(isTransmitting);

        var lastAudioTypeProp = captainTrack.GetType().GetProperty("LastAudioType");
        byte lastAudioType = (byte)lastAudioTypeProp!.GetValue(captainTrack)!;
        Assert.Equal(0x03, lastAudioType);
    }

    [Fact]
    public async Task AudioPlaybackService_ShouldDelayRadioPackets()
    {
        // GIVEN
        var service = new AudioPlaybackService();
        var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)) { ReadFully = true };
        var mixerField = typeof(AudioPlaybackService).GetField("_mixer", BindingFlags.NonPublic | BindingFlags.Instance);
        mixerField!.SetValue(service, mixer);

        service.EnableRadioDelay = true;

        var encoder = Concentus.OpusCodecFactory.CreateEncoder(48000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
        var pcm = new short[960];
        var outBuf = new byte[4000];
        int encodedLen = encoder.Encode(pcm, pcm.Length, outBuf, outBuf.Length);
        var validOpus = new byte[encodedLen];
        Array.Copy(outBuf, validOpus, encodedLen);

        // Distance 10000m (10km) -> 33ms delay
        for (ushort seq = 42; seq <= 44; seq++)
        {
            service.ReceiveOpusFrame(
                playerName: "DelayedBob",
                opusData: validOpus,
                audioType: 0x01, // Radio
                applyRadioEffect: true,
                metadata: null,
                distance: 10000.0,
                speakerZone: "Stanton",
                listenerZone: "Stanton",
                seq: seq,
                isIntercom: false
            );
        }

        var tracksField = typeof(AudioPlaybackService).GetField("_tracks", BindingFlags.NonPublic | BindingFlags.Instance);
        var tracks = (System.Collections.IDictionary)tracksField!.GetValue(service)!;

        Assert.True(tracks.Contains("DelayedBob"));
        var track = tracks["DelayedBob"]!;

        var delayedPacketsField = track.GetType().GetProperty("DelayedPackets");
        var delayedPackets = (System.Collections.IList)delayedPacketsField!.GetValue(track)!;

        // Verify packets are delayed
        Assert.Equal(3, delayedPackets.Count);

        var tickMethod = typeof(AudioPlaybackService).GetMethod("TickPlayback", BindingFlags.NonPublic | BindingFlags.Instance);

        // Tick immediately: packets should NOT be processed yet as delay hasn't elapsed
        tickMethod!.Invoke(service, null);
        Assert.Equal(3, delayedPackets.Count);

        // Wait for 50ms so that playableTime (33ms) has passed
        await Task.Delay(50);

        // Tick again: packets should be dispatched to Jitter Buffer and removed from DelayedPackets
        tickMethod!.Invoke(service, null);
        Assert.Empty(delayedPackets);

        var isTransmittingProp = track.GetType().GetProperty("IsTransmitting");
        bool isTransmitting = (bool)isTransmittingProp!.GetValue(track)!;
        Assert.True(isTransmitting);

        var lastAudioTypeProp = track.GetType().GetProperty("LastAudioType");
        byte lastAudioType = (byte)lastAudioTypeProp!.GetValue(track)!;
        Assert.Equal(0x01, lastAudioType);
    }
}
