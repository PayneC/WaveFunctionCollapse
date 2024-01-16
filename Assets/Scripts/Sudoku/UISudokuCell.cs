using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UISudokuCell : MonoBehaviour
{
    public int nIndex;
    public Image mBackgroup;
    public TMP_InputField mText;
    public TextMeshProUGUI mText2;

    public void SetIndex(int index)
    {
        nIndex = index;
        int2 pos = math.int2(nIndex % 9, nIndex / 9);
        int2 area = pos / 3;

        int areaIndex = area.y * 3 + area.x;
        if(areaIndex % 2 == 0)
        {
            mBackgroup.color = new Color32(255, 159, 0, 255);
        }
        else
        {
            mBackgroup.color = new Color32(255, 72, 0, 255);
        }
    }

    public void SetData(int v, int C)
    {
        if(v > 0 && v < 10)
        {
            mText.text = v.ToString();
        }
        else
        {
            mText.text = null;
        }
        mText2.text = C.ToString();
    }

    public int GetData()
    {
        if(string.IsNullOrWhiteSpace(mText.text))
            return 0;
        if(int.TryParse(mText.text, out nIndex)) 
            return nIndex;
        return 0;
    }
}
