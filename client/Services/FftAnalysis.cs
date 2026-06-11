using System;

namespace XuruVoipClient.Services;

/// <summary>
/// A lightweight, high-performance 64-point Cooley-Tukey Radix-2 FFT implementation.
/// </summary>
public static class FftAnalysis
{
    public const int FftSize = 64;
    private static readonly float[] CosTable = new float[FftSize];
    private static readonly float[] SinTable = new float[FftSize];
    private static readonly int[] BitReverseTable = new int[FftSize];

    static FftAnalysis()
    {
        // Precompute twiddle factors and bit reversal table for N=64
        for (int i = 0; i < FftSize; i++)
        {
            double angle = -2.0 * Math.PI * i / FftSize;
            CosTable[i] = (float)Math.Cos(angle);
            SinTable[i] = (float)Math.Sin(angle);

            // Compute bit reversal for size 64 (6 bits, 2^6 = 64)
            int rev = 0;
            int temp = i;
            for (int bit = 0; bit < 6; bit++)
            {
                rev = (rev << 1) | (temp & 1);
                temp >>= 1;
            }
            BitReverseTable[i] = rev;
        }
    }

    /// <summary>
    /// Computes the 64-point FFT of a real input array.
    /// </summary>
    public static void ComputeFft(float[] realIn, float[] realOut, float[] imagOut)
    {
        if (realIn == null || realOut == null || imagOut == null)
            throw new ArgumentNullException();
        if (realIn.Length < FftSize || realOut.Length < FftSize || imagOut.Length < FftSize)
            throw new ArgumentException($"Buffers must be at least size {FftSize}.");

        // 1. Bit-reversal permutation
        for (int i = 0; i < FftSize; i++)
        {
            int rev = BitReverseTable[i];
            realOut[rev] = realIn[i];
            imagOut[rev] = 0f;
        }

        // 2. Cooley-Tukey Radix-2 FFT stages
        for (int size = 2; size <= FftSize; size <<= 1)
        {
            int halfSize = size >> 1;
            int tabStep = FftSize / size;

            for (int i = 0; i < FftSize; i += size)
            {
                for (int j = 0; j < halfSize; j++)
                {
                    int k = i + j;
                    int l = k + halfSize;

                    // Twiddle factors
                    int tIdx = j * tabStep;
                    float wr = CosTable[tIdx];
                    float wi = SinTable[tIdx];

                    // Complex multiplication: t = w * out[l]
                    float tr = realOut[l] * wr - imagOut[l] * wi;
                    float ti = realOut[l] * wi + imagOut[l] * wr;

                    // Butterfly update
                    realOut[l] = realOut[k] - tr;
                    imagOut[l] = imagOut[k] - ti;
                    realOut[k] += tr;
                    imagOut[k] += ti;
                }
            }
        }
    }
}
