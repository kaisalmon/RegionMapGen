using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

public class TileSelection{
	public HashSet<Tile> tiles;
	public TileSelection(){
		this.tiles = new HashSet<Tile>();
	}
	public TileSelection(HashSet<Tile> tiles){
		this.tiles = new HashSet<Tile>(tiles);
	}
	public TileSelection(List<Tile> tiles){
		this.tiles = new HashSet<Tile>();
		foreach(var t in tiles){
			this.tiles.Add(t);
		}
	}
	public TileSelection(TileSelection selection){
		tiles = new HashSet<Tile>(selection.tiles);
	}
	public Tile this[int i]
	{
	    get { return this.ToList()[i]; }
	    set { this.tiles.Add(value);}
	}

	public List<Tile> ToList(){
		return new List<Tile>(tiles);
	}
	public TileSelection FindAll(Predicate<Tile> test){
		return new TileSelection(ToList().FindAll(test));
	}
	public bool Any(Predicate<Tile> test){
		return ToList().Find(test) != null;
	}
	public bool Contains(Tile t){
		return tiles.Contains(t);
	}
	public bool DoesNotContain(Tile t){
		return !(tiles.Contains(t));
	}
	public bool Add(Tile t){
		return tiles.Add(t);
	}
	public bool Remove(Tile t){
		return tiles.Remove(t);
	}
	public int Count(){
		return tiles.Count;
	}
	public Tile Sample(){
		var list = this.ToList();
		list.Sort((a, b) => (int)(3 - 6 * UnityEngine.Random.value));
		return list[0];
	}
}
