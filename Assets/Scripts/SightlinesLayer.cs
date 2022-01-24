using System.Collections.Generic;
using UnityEngine;

public class SightlinesLayer : MonoBehaviour
{
    public List<string> layerNames = new List<string>();

    private void OnEnable()
    {
        //Camera.main.GetComponent<SightlinesManager>()
    }
}
