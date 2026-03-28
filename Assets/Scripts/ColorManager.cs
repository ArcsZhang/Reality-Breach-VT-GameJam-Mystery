using System.Collections.Generic;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    public enum m_ColorState { Red, Green, Blue, Orange, Yellow, Gray, Black }
    
    private GameObject m_Object;
    private Renderer m_Renderer;

    private Dictionary<Color, m_ColorState> m_ColorMap = new()
    {
        { Color.red, m_ColorState.Red },
        { Color.green, m_ColorState.Green },
        { Color.blue, m_ColorState.Blue },
        { Color.orange, m_ColorState.Orange },
        { Color.yellow, m_ColorState.Yellow },
        { Color.gray, m_ColorState.Gray },
        { Color.black, m_ColorState.Black }

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { 
    
    }

    // Update is called once per frame
    void Update()
    {
        Color current = GetComponent<Renderer>().material.color;
        m_ColorState state = m_ColorMap[current];

        switch(state)
        {
            case m_ColorState.Red:
                Red();
                break;
            case m_ColorState.Green:
                Green();
                break;
            case m_ColorState.Blue:
                Blue();
                break;
            case m_ColorState.Orange:
                Orange();
                break;
            case m_ColorState.Yellow:
                Yellow();
                break;
            case m_ColorState.Gray:
                Gray();
                break;
            case m_ColorState.Black:
                Black();
                break;
            default:
                Miscellaneous();
                break;
        }

    private void Red()
    {

    }

    private void Green()
    {

    }

    private void Blue()
    {

    }

    private void Yellow()
    {

    }

    private void Orange()
    {

    }

    private void Gray()
    {

    }

    private void Black()
    {

    }

    private void Miscellaneous()
    {

    }    
}
