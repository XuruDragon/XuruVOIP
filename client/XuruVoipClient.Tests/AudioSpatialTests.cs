using System;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class AudioSpatialTests
{
    [Fact]
    public void CalculateSpatialParams_DistanceAttenuation_ShouldFollowLinearRollOff()
    {
        // GIVEN
        double listenerX = 0, listenerY = 0, listenerZ = 0;
        double headingX = 0, headingY = 1;
        double speakerX = 0, speakerY = 0, speakerZ = 0; // coordinates don't affect distance if spatial is false
        float maxRange = 50.0f;

        // WHEN (At 0 meters)
        var result0 = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            speakerX, speakerY, speakerZ, 0f, maxRange, false, false);

        // THEN
        Assert.Equal(1.0f, result0.volumeFactor);
        Assert.Equal(0.0f, result0.pan);

        // WHEN (At 25 meters - half distance)
        var result25 = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            speakerX, speakerY, speakerZ, 25f, maxRange, false, false);

        // THEN
        Assert.Equal(0.5f, result25.volumeFactor);
        Assert.Equal(0.0f, result25.pan);

        // WHEN (At >= 50 meters)
        var result50 = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            speakerX, speakerY, speakerZ, 50f, maxRange, false, false);

        // THEN
        Assert.Equal(0.0f, result50.volumeFactor);
    }

    [Fact]
    public void CalculateSpatialParams_PanningAndFrontBackAttenuation_ShouldRotateClockwise()
    {
        // GIVEN: Listener at origin (0, 0) facing North (0, 1)
        double listenerX = 0, listenerY = 0, listenerZ = 0;
        double headingX = 0, headingY = 1;
        float maxRange = 50f;

        // 1. Speaker directly in Front (0, 10)
        double frontX = 0, frontY = 10, frontZ = 0;
        var frontResult = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            frontX, frontY, frontZ, 10f, maxRange, true, true);

        // THEN
        Assert.InRange(frontResult.pan, -0.05f, 0.05f); // centered
        Assert.Equal(1.0f - (10f / maxRange), frontResult.volumeFactor); // full volume for 10m

        // 2. Speaker directly on Right (10, 0)
        double rightX = 10, rightY = 0, rightZ = 0;
        var rightResult = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            rightX, rightY, rightZ, 10f, maxRange, true, true);

        // THEN
        Assert.InRange(rightResult.pan, 0.95f, 1.0f); // fully right

        // 3. Speaker directly Behind (0, -10)
        double behindX = 0, behindY = -10, behindZ = 0;
        var behindResult = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            behindX, behindY, behindZ, 10f, maxRange, true, true);

        // THEN
        Assert.InRange(behindResult.pan, -0.05f, 0.05f); // centered
        // Behind factor should apply a 25% volume drop (volumeFactor * 0.75)
        float expectedVolume = (1.0f - (10f / maxRange)) * 0.75f;
        Assert.InRange(behindResult.volumeFactor, expectedVolume - 0.01f, expectedVolume + 0.01f);

        // 4. Speaker directly on Left (-10, 0)
        double leftX = -10, leftY = 0, leftZ = 0;
        var leftResult = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            leftX, leftY, leftZ, 10f, maxRange, true, true);

        // THEN
        Assert.InRange(leftResult.pan, -1.0f, -0.95f); // fully left
    }

    [Fact]
    public void CalculateSpatialParams_DisabledSpatial_ShouldStayCentered()
    {
        // GIVEN: Listener at origin (0, 0) facing North (0, 1), Speaker on Right (10, 0)
        double listenerX = 0, listenerY = 0, listenerZ = 0;
        double headingX = 0, headingY = 1;
        double speakerX = 10, speakerY = 0, speakerZ = 0;
        float maxRange = 50f;

        // WHEN: Client spatial is disabled
        var clientDisabledResult = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            speakerX, speakerY, speakerZ, 10f, maxRange, true, false);

        // THEN
        Assert.Equal(0.0f, clientDisabledResult.pan);

        // WHEN: Server spatial is disabled
        var serverDisabledResult = AudioPlaybackService.CalculateSpatialParams(
            listenerX, listenerY, listenerZ, headingX, headingY,
            speakerX, speakerY, speakerZ, 10f, maxRange, false, true);

        // THEN
        Assert.Equal(0.0f, serverDisabledResult.pan);
    }
}
