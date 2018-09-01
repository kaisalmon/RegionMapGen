using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class Region {
  public List<Tile> tiles = new List<Tile>();
  public Color color;

  public void Assign(Tile t){
    if(t.region != null){
      t.region.tiles.Remove(t);
    }
    this.tiles.Add(t);
    t.region = this;
  }

	public Region ClosestRegion(){
    if(this.tiles.Count == 0){
      throw new Exception("Empty Regions cant have a closest Region");
    }
    return MapGen.ClosestRegion(this.tiles, this);
  }

  public void MergeInto(Region r){
    while(tiles.Count > 0){
      r.Assign(tiles[0]);
    }
  }

  public void Split(Region r, int fraction = 2){
    var target = this.tiles.Count / (float)fraction;
    var i = 0;
    var original_fragments = MapGen.SeperateContiguous(this.tiles).Count;
    var open = new List<Tile>();
    var start = tiles[(int)(UnityEngine.Random.value*tiles.Count)];
    open.Add(start);
    while(r.tiles.Count < target && open.Count > 0){
      i++;
      var t = open[(int)(UnityEngine.Random.value*open.Count)];
      open.Remove(t);

      r.Assign(t);
      if(i % 50 == 1){
        JoinCutOffAreas(r,original_fragments);
      }
      foreach(var n in t.adjacent){
        if(n.region == this && open.IndexOf(n) == -1){
          if(n.land){
            open.Add(n);
          }
        }
      }
    }
    this.JoinCutOffAreas(r,original_fragments);
    var core = MapGen.Grow(MapGen.Shrink(r.tiles));
    var to_switch_back = new List<Tile>();

    foreach(var t in r.tiles){
      if(core.IndexOf(t)==-1){
        to_switch_back.Add(t);
      }
    }


    foreach(var t in to_switch_back){
      t.region = null;
      r.tiles.Remove(t);
      List<Tile> t_as_list = new List<Tile>();
      t_as_list.Add(t);
      MapGen.ClosestRegion(t_as_list).Assign(t);
    }
    JoinCutOffAreas(r,original_fragments);
  }

  private void JoinCutOffAreas(Region r, int original_fragments){
    var contig_split = MapGen.SeperateContiguous(this.tiles);
    if(contig_split.Count > original_fragments){
      var to_merge = contig_split.Count - original_fragments;
      contig_split.Sort((a,b) => a.Count.CompareTo(b.Count));
      var i = 0;
      while(to_merge > 0 && i < contig_split.Count){
        var adjacent_to_r = false;
        foreach(var t in contig_split[i]){
          if(t.adjacent.FindAll((n)=>n.region == r).Count != 0){
            adjacent_to_r = true;
          }
        }
        if(adjacent_to_r){
          foreach(var tt in contig_split[i]){
            r.Assign(tt);
          }
          to_merge--;
        }
        i++;
      }
    }
  }

}
