using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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
        if(mDatas.IsCreated) { mDatas.Dispose(); }
        if(mTmp.IsCreated) { mTmp.Dispose(); }
    }

    public void Next()
    {
        mTmp.CopyFrom(mDatas);
        new SudokuJob()
        {
            mDestination = mDatas,
            mSource = mTmp,
        }.Schedule(81, 9).Complete();
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

public struct SudokuJob : IJobParallelFor
{
    [ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<SudokuPacket> mSource;
    public NativeArray<SudokuPacket> mDestination;

    public void Execute(int index)
    {
        SudokuPacket packet = mDestination[index];
        if (packet.Result == 0)
        {
            int count = 0;
            int rlts = 0;
            int2 pos = new int2(index % 9, index / 9);
            Area(ref count, ref rlts, pos);
            Row(ref count, ref rlts, pos);
            Column(ref count, ref rlts, pos);

            int rlt = 0;
            if(9 - count < 2)
            {
                for(int i = 0; i < 9; i++) 
                {
                    int b = 1 << i;
                    if((rlts & b) == 0)
                    {
                        rlt = i + 1;
                        break;
                    }
                }
            }

            packet.Entropy = (byte)count;
            packet.Result = (byte)rlt;
            packet.PossibleResults = (byte)rlts;

            mDestination[index] = packet;
        }
    }

    private void AppendResult(ref int count, ref int rlts, SudokuPacket packet)
    {
        if (packet.Result == 0)
            return;

        int resultBit = 1 << (packet.Result - 1);
        if ((resultBit & rlts) == 0)
        {
            rlts |= resultBit;
            ++count;
        }
    }

    public void Area(ref int count, ref int rlts, int2 pos) 
    {
        int2 area = pos / 3;

        int2 min = area * 3;
        int2 max = area * 3 + 3;

        for(int y = min.y; y < max.y; ++y)
        {
            for (int x = min.x; x < max.x; ++x)
            {
                SudokuPacket packet = mSource[x + y * 9];
                AppendResult(ref count, ref rlts, packet);
            }
        }
    }

    //ÐÐ
    private void Row(ref int count, ref int rlts, int2 pos)
    {
        for (int x = 0; x < 9; ++x)
        {
            SudokuPacket packet = mSource[x + pos.y * 9];
            AppendResult(ref count, ref rlts, packet);
        }
    }

    private void Column(ref int count, ref int rlts, int2 pos)
    {
        for (int y = 0; y < 9; ++y)
        {
            SudokuPacket packet = mSource[pos.x + y * 9];
            AppendResult(ref count, ref rlts, packet);
        }
    }
}
