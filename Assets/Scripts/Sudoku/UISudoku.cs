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


        for (int i = 0; i < 81; ++i)
        {
            mCells[i] = GameObject.Instantiate(mPrefab, mGrid.transform).GetComponent<UISudokuCell>();
            mCells[i].SetIndex(i);
            mCells[i].SetData(d[i]);
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
            mCells[i].SetData(mData.mDatas[i].Result);
        }
    }

    public void Set()
    {
        for (int i = 0; i < 81; ++i)
        {
            SudokuPacket packet = new SudokuPacket();
            packet.Result = (byte)mCells[i].GetData();

            mData.mDatas[i] = packet; 
        }
    }

    public void Clear()
    {
        for (int i = 0; i < 81; ++i)
        {
            mCells[i].SetData(0);

            SudokuPacket packet = new SudokuPacket();
            packet.Result = (byte)mCells[i].GetData();

            mData.mDatas[i] = packet;
        }
    }
}
