using System;
using System.Runtime.InteropServices;
using System.Timers;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;

public class Sudoku : IDisposable
{
    public NativeArray<SudokuPacket> mDatas;
    public NativeArray<SudokuPacket> mTmp;

    public Sudoku()
    {
        mDatas = new NativeArray<SudokuPacket>(81, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        mTmp = new NativeArray<SudokuPacket>(81, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }

    public void Dispose()
    {
        if (mDatas.IsCreated) { mDatas.Dispose(); }
        if (mTmp.IsCreated) { mTmp.Dispose(); }
    }

    public void Next()
    {
        JobHandle jobHandle = new SudokuPossibleJob()
        {
            mSource = mDatas,
            mDestination = mTmp,
        }.Schedule(81, 9);
        jobHandle = new SudokuSettleJob()
        {
            mSource = mTmp,
            mDestination = mDatas,
        }.Schedule(jobHandle);
        jobHandle = new SudokuNecessaryJob()
        {
            mSource = mDatas,
            mDestination = mTmp,
        }.Schedule(81, 9, jobHandle);
        jobHandle = new SudokuSettleJob()
        {
            mSource = mTmp,
            mDestination = mDatas,
        }.Schedule(jobHandle);
        jobHandle.Complete();
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct SudokuPacket
{
    [FieldOffset(0)]
    private int Packet;

    [FieldOffset(0)]
    public byte Entropy;

    [FieldOffset(1)]
    public byte Result;

    [FieldOffset(2)]
    public ushort PossibleResults;
}

/// <summary>
/// 计算每个格子可能的结果
/// </summary>
public struct SudokuPossibleJob : IJobParallelFor
{
    [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<SudokuPacket> mSource;
    public NativeArray<SudokuPacket> mDestination;

    public void Execute(int index)
    {
        SudokuPacket packet = mSource[index];
        if (packet.Result == 0)
        {
            int rlts = 0;
            int2 pos = new int2(index % 9, index / 9);
            Area(ref rlts, pos);
            Row(ref rlts, pos);
            Column(ref rlts, pos);

            packet.PossibleResults = (ushort)~rlts;
        }
        mDestination[index] = packet;
    }

    private void AppendResult(ref int rlts, SudokuPacket packet)
    {
        if (packet.Result == 0)
            return;

        int resultBit = 1 << (packet.Result - 1);
        rlts |= resultBit;
    }

    public void Area(ref int rlts, int2 pos)
    {
        int2 area = pos / 3;

        int2 min = area * 3;
        int2 max = area * 3 + 3;

        for (int y = min.y; y < max.y; ++y)
        {
            for (int x = min.x; x < max.x; ++x)
            {
                SudokuPacket packet = mSource[x + y * 9];
                AppendResult(ref rlts, packet);
            }
        }
    }

    //行
    private void Row(ref int rlts, int2 pos)
    {
        for (int x = 0; x < 9; ++x)
        {
            SudokuPacket packet = mSource[x + pos.y * 9];
            AppendResult(ref rlts, packet);
        }
    }

    private void Column(ref int rlts, int2 pos)
    {
        for (int y = 0; y < 9; ++y)
        {
            SudokuPacket packet = mSource[pos.x + y * 9];
            AppendResult(ref rlts, packet);
        }
    }
}

/// <summary>
/// 计算每个格子必须是哪些结果
/// 将一个9宫格内其他格子的所有可能性相加之后取反
/// 得到当前格子必须是哪些结果
/// 并且与自身可能的结果进行交集
/// </summary>
public struct SudokuNecessaryJob : IJobParallelFor
{
    [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<SudokuPacket> mSource;
    public NativeArray<SudokuPacket> mDestination;

    public void Execute(int index)
    {
        SudokuPacket packet = mSource[index];
        if (packet.Result == 0)
        {
            int rlts = 0;
            int2 pos = new int2(index % 9, index / 9);
            Area(ref rlts, pos);

            rlts = ~rlts;
            if ((ushort)rlts != 0)
            {
                packet.PossibleResults &= (ushort)rlts;
            }

            rlts = 0;
            Row(ref rlts, pos);

            rlts = ~rlts;
            if ((ushort)rlts != 0)
            {
                packet.PossibleResults &= (ushort)rlts;
            }

            rlts = 0;
            Column(ref rlts, pos);

            rlts = ~rlts;
            if ((ushort)rlts != 0)
            {
                packet.PossibleResults &= (ushort)rlts;
            }
        }
        mDestination[index] = packet;
    }

    public void Area(ref int rlts, int2 pos)
    {
        int2 area = pos / 3;

        int2 min = area * 3;
        int2 max = area * 3 + 3;

        for (int y = min.y; y < max.y; ++y)
        {
            for (int x = min.x; x < max.x; ++x)
            {
                if (x == pos.x && y == pos.y)
                    continue;

                SudokuPacket packet = mSource[x + y * 9];
                rlts |= packet.PossibleResults;
            }
        }
    }

    //行
    private void Row(ref int rlts, int2 pos)
    {
        for (int x = 0; x < 9; ++x)
        {
            if (x == pos.x)
                continue;
            SudokuPacket packet = mSource[x + pos.y * 9];
            rlts |= packet.PossibleResults;
        }
    }

    private void Column(ref int rlts, int2 pos)
    {
        for (int y = 0; y < 9; ++y)
        {
            if (y == pos.y)
                continue;
            SudokuPacket packet = mSource[pos.x + y * 9];
            rlts |= packet.PossibleResults;
        }
    }
}

public struct SudokuSettleJob : IJob
{
    [ReadOnly] public NativeArray<SudokuPacket> mSource;
    public NativeArray<SudokuPacket> mDestination;

    public int Cal(in int rlts, out int rlt)
    {
        rlt = 0;

        int count = 0;
        for (int i = 0; i < 9; ++i)
        {
            int b = 1 << i;
            if ((rlts & b) == b)
            {
                rlt = i + 1;
                ++count;
            }
        }
        return count;
    }

    public void Execute()
    {
        for (int i = 0; i < 81; ++i)
        {
            SudokuPacket packet = mSource[i];
            if (packet.Result == 0)
            {
                packet.Entropy = (byte)Cal(packet.PossibleResults, out int rlt);
                if (1 == packet.Entropy)
                {
                    packet.Result = (byte)rlt;
                }
            }
            mDestination[i] = packet;
        }
    }
}