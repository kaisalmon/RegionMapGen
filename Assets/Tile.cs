using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Tile : MonoBehaviour {
  private static Color LAND_COLOR = new Color(1,1,1);
  private static Color SEA_COLOR = new Color(0,0,0);
  private static Color SELECTED_R_COLOR = new Color(1,0.9f,0);
  private static Color SELECTED_T_COLOR = new Color(1,0.4f,0);
  public GuiManager gui_manager;
	public List<Tile> adjacent = new List<Tile>();
  public Region region;
  public int x, y;
  public bool land = false;
	// Use this for initialization
	void Start () {

	}

  void OnMouseDown()
  {
    gui_manager.selected_region = this.region; //Even if null
  }

  public bool IsBorder(){
    if(region == null){
      return false;
    }
    if(adjacent.FindAll((t)=>t.region != this.region).Count > 0){
      return true;
    }
    return adjacent.Count != 4;
  }

	// Update is called once per frame
	void Update () {
    if(gui_manager.selected_tiles.IndexOf(this) != -1){
      this.GetComponent<SpriteRenderer>().color = SELECTED_T_COLOR;
      return;
    }

    if(region != null && gui_manager.selected_region == this.region){
      this.GetComponent<SpriteRenderer>().color = SELECTED_R_COLOR;
    }else{
      if(this.IsBorder()){
        this.GetComponent<SpriteRenderer>().color = this.region.color;
      }else{
        this.GetComponent<SpriteRenderer>().color = this.land ? LAND_COLOR : SEA_COLOR;
      }
    }
	}
  public string CoordString(){
    return x+", "+y;
  }

	public void Attach(Tile t){
		this.adjacent.Add(t);
		t.adjacent.Add(this);
	}
}
