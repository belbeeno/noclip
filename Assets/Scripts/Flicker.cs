using UnityEngine;

// Pulled from https://gist.github.com/LotteMakesStuff/9612c5201d0308f86552f0600ac74802

public class Flicker : MonoBehaviour
{
    public float LightIntensityMin = 5f;
    public float LightIntensityMax = 10f;
    public string LightStyle = "mmamammmmammamamaaamammma";
    [SerializeField]
    private new Light light;

    public float loopTime = 2f;
    [SerializeField]
    private int currentIndex = 0;
    private float lightTimer;

    private void Start()
    {
        lightTimer = Random.Range(0f, loopTime);
    }

    void Update()
    {
        char c = GetNextChar();
        int val = c - 'a';
        float intensity = (val / 25f) * 2;
        light.intensity = LightIntensityMin + Mathf.Lerp(0f, LightIntensityMax - LightIntensityMin, intensity);
    }


    private char GetNextChar()
    {
        lightTimer += Time.deltaTime;
        var step = loopTime / LightStyle.Length;

        if (step < lightTimer)
        {
            lightTimer -= step;
            currentIndex++;
            if (currentIndex >= LightStyle.Length)
                currentIndex = 0;
        }

        return LightStyle[currentIndex];
    }
}