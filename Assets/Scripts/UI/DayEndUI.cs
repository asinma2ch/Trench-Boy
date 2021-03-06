﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DayEndUI : MonoBehaviour
{
    public Text Text_Head;
    public Text Dead_List;
    public bool AddRIP;

    public float UI_speed;

    public int Day = 0;
    public int Allies_Remaining;

    GameController gc;

    public bool ui_up = false;
    public bool ui_down = false;

    void Start()
    {
        gc = GameObject.Find("GameControl").GetComponent<GameController>();
    }
    
    void Update()
    {
        Text_Head.text = "REPORT " + "DAY " + ((int)gc.CurrentDay + 1) + "\n" +
                         "Remaining Allies = " + Allies_Remaining;
        if (ui_up)
        {
            this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + UI_speed, this.transform.position.z);

            if (this.transform.localPosition.y >= -300.0f)
            {
                if (this.transform.localPosition.y > -300.0f)
                {
                    this.transform.localPosition = new Vector3(this.transform.localPosition.x, -300.0f, this.transform.position.z);
                }

                ui_up = false;
            }
        }

        if (ui_down)
        {
            this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y - UI_speed, this.transform.position.z);

            if (this.transform.localPosition.y <= -1500.0f)
            {
                if (this.transform.localPosition.y < -1500.0f)
                {
                    this.transform.localPosition = new Vector3(this.transform.localPosition.x, -1500.0f, this.transform.position.z);
                }

                //this.SetActive(false);
                ui_down = false;
            }
        }

    }

    

    public void addDeadList(bool alldead)
    {
        Dead_List.text = "Casualties List:\n";

        if (!alldead)
        {
            for (int i = 0; i < GameController.AlliesDieToday.Length; i++)
            {
                if (GameController.AlliesDieToday[i])
                {
                    Dead_List.text += GameController.AlliesName[i];
                    Dead_List.text += "\n";
                }

            }
        }
        else
        {
            for (int i = 0; i < GameController.AlliesName.Length; i++)
            {
                Dead_List.text += GameController.AlliesName[i];
                Dead_List.text += "\n";
            }
        }
    }
}
