using UnityEngine;
using UnityEngine.UI;


public class UISudoku : MonoBehaviour
{
    public GridLayoutGroup mGrid;
    public GameObject mPrefab;
    public UISudokuCell[] mCells = new UISudokuCell[81];
    public Sudoku mData;

    private void Awake()
    {
        mData = new Sudoku();
    }

    private void Start()
    {
        int[] d = new int[81]
        {
            6,2,0,0,0,1,7,0,8,
            0,0,0,0,7,0,3,0,5,
            0,7,1,5,0,0,4,0,0,
            8,0,0,1,0,7,9,0,0,
            0,4,0,0,3,0,0,7,0,
            0,0,6,4,0,9,0,0,1,
            0,0,7,0,0,6,2,4,0,
            2,0,3,0,8,0,0,0,0,
            9,0,4,2,0,0,0,6,7,
        };

        int[] d2 = new int[81]
        {
            9,7,0,4,0,0,0,0,0,
            0,2,0,0,0,1,9,0,0,
            0,0,0,0,0,6,0,0,8,
            0,0,5,0,0,9,0,0,0,
            0,0,0,3,0,0,8,0,1,
            0,0,0,0,8,0,7,3,0,
            5,0,0,0,0,0,0,0,9,
            0,0,3,8,5,0,4,0,0,
            6,0,2,0,0,0,0,5,0,
        };


        for (int i = 0; i < 81; ++i)
        {
            mCells[i] = GameObject.Instantiate(mPrefab, mGrid.transform).GetComponent<UISudokuCell>();
            mCells[i].SetIndex(i);
            mCells[i].SetData(d2[i], d2[i] == 0 ? 9 : 1);
        }
        mPrefab.SetActive(false);

        Set();
    }

    private void OnDestroy()
    {
        mData?.Dispose();
    }

    public void Next()
    {
        mData.Next();
        for (int i = 0; i < 81; ++i)
        {
            mCells[i].SetData(mData.mDatas[i].Result, mData.mDatas[i].Entropy);
        }
    }

    public void Set()
    {
        for (int i = 0; i < 81; ++i)
        {
            SudokuPacket packet = new SudokuPacket();
            packet.Result = (byte)mCells[i].GetData();
            if(packet.Result != 0)
            {
                packet.Entropy = 1;
                packet.PossibleResults = (ushort)(1 << ((int)packet.Result - 1));
            }
            else
            {
                packet.Entropy = 9;
                packet.PossibleResults = 0;
            }
            mData.mDatas[i] = packet; 
        }
    }

    public void Clear()
    {
        for (int i = 0; i < 81; ++i)
        {
            mCells[i].SetData(0, 0);

            SudokuPacket packet = new SudokuPacket();
            packet.Result = (byte)mCells[i].GetData();

            mData.mDatas[i] = packet;
        }
    }
}
