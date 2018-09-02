using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Tile : MonoBehaviour {
  private static Color LAND_COLOR = new Color(146/255f, 186/255f, 89/255f);
  private static Color SEA_COLOR = new Color(0,0,0);
  private static Color SELECTED_R_COLOR = new Color(1,0.9f,0);
  private static Color SELECTED_T_COLOR = new Color(1,0.4f,0);

  public GuiManager gui_manager;
	public TileSelection adjacent = new TileSelection();
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
    if(adjacent.FindAll((t)=>t.region != this.region).Count() > 0){
      return true;
    }
    return adjacent.Count() != 4;
  }

	// Update is called once per frame
	void Update () {
    var rend = GetComponent<MeshRenderer>();
    rend.enabled = this.land;
    if(gui_manager.selected_region == this.region && gui_manager.selected_region != null){
        rend.material.color = SELECTED_R_COLOR;
        return;
    }
    rend.material.color = LAND_COLOR;
	}
  public string CoordString(){
    return x+", "+y;
  }

	public void Attach(Tile t){
		this.adjacent.Add(t);
		t.adjacent.Add(this);
	}
}
