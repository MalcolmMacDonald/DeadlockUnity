using EasyButtons;
using UnityEngine;

public class PositionRemapper : MonoBehaviour
{
    //initial deadlock pose:
    //-491.97, -10699.67, 1279.03
    //initial unity pos
    //271.8, 32.98, -12.50

    //unity Z = deadlock X,
    //unity X  = deadlock Y,
    //unity  Y  = deadlock Z


    //second y = -8937.66
    //second unity x = 226.47

    //unity X offset = 8.67
    //deadlock Y offset = 1762.01

    //deadlock y factor = 203.230681;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    [Button]
    public void PrintPosition()
    {
        var unityPos = transform.position;
        var deadlockPos = new Vector3(unityPos.z * 39.487f, unityPos.x * -39.2309761f, unityPos.y * 39.21f);
        print($"setpos_exact {deadlockPos.x:F2} {deadlockPos.y:F2} {deadlockPos.z:F2}");
    }
}