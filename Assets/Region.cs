using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class Region {
  public TileSelection tiles = new TileSelection();
  public Color color;

  public void Assign(Tile t){
    if(t.region != null){
      t.region.tiles.Remove(t);
    }
    this.tiles.Add(t);
    t.region = this;
  }

	public Region ClosestRegion(){
    if(this.tiles.Count() == 0){
      throw new Exception("Empty Regions cant have a closest Region");
    }
    return MapGen.ClosestRegion(this.tiles, this);
  }

  public void MergeInto(Region r){
    while(tiles.Count() > 0){
      r.Assign(tiles[0]);
    }
  }

  public void Split(Region r, int fraction = 2){
    var target = this.tiles.Count() / (float)fraction;
    var i = 0;
    var original_fragments = MapGen.SeperateContiguous(this.tiles).Count();
    var open = new TileSelection();
    var start = tiles.Sample();
    open.Add(start);
    while(r.tiles.Count() < target && open.Count() > 0){
      i++;
      var t = open.Sample();
      open.Remove(t);

      r.Assign(t);
      if(i % 10 == 1){
        JoinCutOffAreas(r,original_fragments);
      }
      foreach(var n in t.adjacent.tiles){
        if(n.region == this){
          if(n.land){
            open.Add(n);
          }
        }
      }
    }

    this.JoinCutOffAreas(r,original_fragments);
    var core = r.tiles;
    //core = MapGen.Shrink(core);
    var original_core = this.tiles;
    //original_core = MapGen.Shrink(original_core);

    var to_switch_back = new TileSelection();
    foreach(var t in r.tiles.tiles){
      if(core.DoesNotContain(t)){
        to_switch_back.Add(t);
      }
    }
    foreach(var t in this.tiles.tiles){
      if(original_core.DoesNotContain(t)){
        to_switch_back.Add(t);
      }
    }

    Dictionary<Tile, Region> switchMap = new Dictionary<Tile, Region>();
    var allowed_regions = new HashSet<Region>();
    allowed_regions.Add(r);
    allowed_regions.Add(this);
    foreach(var t in to_switch_back.tiles){
      t.region = null;
      r.tiles.Remove(t);
       TileSelection t_as_list = new TileSelection();
       t_as_list.Add(t);
       try{
         switchMap.Add(t, MapGen.ClosestRegion(t_as_list, null, allowed_regions));
       }catch(Exception e){
         Debug.LogError(e); // Swallow
       }
    }
    foreach(KeyValuePair<Tile, Region> entry in switchMap){
      entry.Value.Assign(entry.Key);
    }
    r.JoinCutOffAreas(this, 1);
    this.JoinCutOffAreas(r,original_fragments);
  }

  private bool JoinCutOffAreas(Region r, int original_fragments){
    var made_change = false;
    var contig_split = MapGen.SeperateContiguous(this.tiles);
    Debug.Log(">> "+original_fragments);
    original_fragments = 1;
    if(contig_split.Count() > original_fragments){
      var to_merge = contig_split.Count() - original_fragments;
      contig_split.Sort((a,b) => a.Count().CompareTo(b.Count()));
      var i = 0;
      while(to_merge > 0 && i < contig_split.Count()){
        var adjacent_to_r = true;
        foreach(var t in contig_split[i].tiles){
          if(t.adjacent.FindAll((n)=>n.region == r).Count() != 0){
            adjacent_to_r = true;
          }
        }
        if(adjacent_to_r){
          foreach(var tt in contig_split[i].tiles){
            r.Assign(tt);
            made_change = true;
          }
          to_merge--;
        }
        i++;
        if(i > 1000){
          throw new Exception("Joining cut off area took too long");
        }
      }
    }
    return made_change;
  }

}
