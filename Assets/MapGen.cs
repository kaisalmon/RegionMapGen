using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MapGen : MonoBehaviour {
	private static GuiManager _gui_manager;
	public GuiManager gui_manager;
	public Tile base_tile;
	public int width = 10, height = 10;
	public float land_threshold = 0.7f;
	public float scale = 0.2f;
	public float x_offset = 0;
	public float y_offset = 0;
	public int seed = 0;
	public Texture2D heightmap;

	public int min_size = 15;
	public int max_size = 35;

	private Tile[,] grid;
	private List<Region> regions = new List<Region>();

	private static float last_timestamp = -1;
	private static string last_marker = "Pre Init";
	private static Dictionary<string, double> times = new Dictionary<string, double>();

	public static void Stamp(string new_marker){
		var t = Time.realtimeSinceStartup;
		if(last_timestamp!=-1){
			Debug.Log(last_marker+": "+(t - last_timestamp));
		}
		last_timestamp = t;
		last_marker = new_marker;
	}

	// Use this for initialization
	public void Start(){
		_gui_manager = gui_manager;

		this.grid = new Tile[width, height];
		UnityEngine.Random.seed = seed;
		float tileWidth = (float)base_tile.GetComponent<Renderer>().bounds.size.x;
		for(var x = 0; x < width; x++){
				for(var y = 0; y < height; y++){
					var pos = this.transform.position;
					pos += new Vector3((x-width/2)*tileWidth,(y-height/2)*tileWidth,0);
					var t = Instantiate(base_tile, pos, Quaternion.identity) as Tile;
					t.x = x;
					t.y = y;
					grid[x,y] = t;
				}
		}
		for(var x = 0; x < width; x++){
			for(var y = 0; y < height; y++){
				if(y!=0)
					grid[x,y].Attach(grid[x,y-1]);
				if(x!=0)
					grid[x,y].Attach(grid[x-1,y]);
			}
		}
		StartCoroutine(this.Generate());
	}


	public IEnumerator TestSplit() {
		var r = new Region();
		r.color = new Color(1,0,0);
		var r2 = new Region();
		r2.color = new Color(0,1,0);
		for(var x = 0; x < width; x++){
			for(var y = 0; y < height; y++){
					grid[x,y].land = true;
					r.Assign(grid[x,y]);
			}
		}
		r.Split(r2);
		yield return new WaitForSeconds(0);
	}

	public IEnumerator Generate() {

		/************************/ Stamp("Init and Preinit");
		for(var x = 0; x < width; x++){
				for(var y = 0; y < height; y++){
					var t = grid[x,y];
					t.land = x!=0;
					// heightmap.GetPixel(x*10, y*10).grayscale < 0.5f;
					//Perlin.OctavePerlin(x*scale+x_offset,y*scale+y_offset,0,7,0.3) < land_threshold;
					t.x = x;
					t.y = y;
					grid[x,y] = t;
					t.GetComponent<SpriteRenderer>().color = new Color(0,0,0);
				}
			}
			/************************/ Stamp("Building Land");
			yield return new WaitForSeconds(0);

			/************************/ Stamp("Finding Land");
			var land = this.All().FindAll((t) => t.land);

			/************************/ Stamp("Shrinking Land");
			land = MapGen.Shrink(land);
			land = MapGen.Shrink(land);
			land = MapGen.Shrink(land);
			/************************/ Stamp("Returning True");
		 	return true;
			/************************/ Stamp("Seperating into base regions");
			var selection_list = SeperateContiguous(land);

			/************************/ Stamp("Expanding into base regions");
			MapGen.ExpandRegions(selection_list);


			/************************/ Stamp("Finding Islands");
			var island_tiles = new List<Tile>();
			foreach(var t in this.All()){
				if(!t.land){
					continue;
				}
				bool in_region = false;
				foreach(var region in selection_list){
					if(region.IndexOf(t)!=-1){
						in_region = true;
					}
				}
				if(!in_region){
					island_tiles.Add(t);
				}
			}
			/************************/ Stamp("Seperating Islands");
			var island_selections = SeperateContiguous(island_tiles);
			selection_list.AddRange(island_selections);

			/*var too_small_region_tiles = new List<List<Tile>>();
			foreach(var region_tiles in selection_list){
				if(region_tiles.Count < min_size){
					too_small_region_tiles.Add(region_tiles);
				}
			}*/


			Color[] colors = new[] {new Color(0.6f,0.1f,0.1f), new Color(0,0.6f,0), new Color(0,0,0.6f),
										new Color(0.6f,0.5f,0), new Color(1,0,1), new Color(0,1,1),
									new Color(0.6f,0.5f,1), new Color(1,0.5f,1), new Color(0,0.3f,0.7f),
								new Color(0.6f,0.5f,0.5f), new Color(0.5f,0,1), new Color(0,0.6f,0.4f)};

			/************************/ Stamp("Founding Region Objects");
			var i = 0;
			foreach(var region_tiles in selection_list){
				i++;
				i = i % colors.Length;
				var region = new Region();
				this.regions.Add(region);
				region.color = colors[i];
				foreach(var t in region_tiles){
					region.Assign(t);
				}
			}
			yield return new WaitForSeconds(0);
			for(var splitcount = 0; splitcount < 20; splitcount++){
				foreach(var too_big_region in this.regions.FindAll((r)=>r.tiles.Count > max_size)){
					/************************/ Stamp("Spliting Region");
					yield return new WaitForSeconds(1);
					i++;
					i = i % colors.Length;
					var region = new Region();
					region.color = colors[i];
					this.regions.Add(region);
					too_big_region.Split(region);
					_gui_manager.selected_tiles = new List<Tile>();
				}
			}
			_gui_manager.selected_tiles = new List<Tile>();

			/************************/ Stamp("Merging in Islands");
			foreach(var too_small_region in this.regions.FindAll((r)=>r.tiles.Count < min_size)){
				yield return new WaitForSeconds(0);
				if(too_small_region.tiles.Count != 0){
					too_small_region.MergeInto(too_small_region.ClosestRegion());
					_gui_manager.selected_tiles = too_small_region.tiles;
				}
			}

		_gui_manager.selected_tiles = new List<Tile>();
		UnityEngine.Profiler.EndSample();
		/************************/ Stamp("Finished");
		yield return new WaitForSeconds(0);
	}

	public List<Tile> All(){
		List<Tile> result = new List<Tile>();
		for(var x = 0; x < width; x++){
				for(var y = 0; y < height; y++){
					result.Add(grid[x,y]);
				}
		}
		return result;
	}

	public static List<Tile> Shrink(List<Tile> tiles){
		List<Tile> result = new List<Tile>();
		foreach(var t in tiles){
			bool valid = true;
			foreach(var n in t.adjacent){
				if(tiles.IndexOf(n) == -1){
					valid = false;
					break;
				}
			}
			if(valid){
				result.Add(t);
			}
		}
		_gui_manager.selected_tiles = result;
		return result;
	}

	public static List<Tile> Grow(List<Tile> tiles){
		var time = Time.realtimeSinceStartup;
		List<Tile> result = new List<Tile>();
		foreach(var t in tiles){
			if(result.IndexOf(t) == -1){
				result.Add(t);
			}
			foreach(var n in t.adjacent){
				if(result.IndexOf(n) == -1){
					result.Add(n);
				}
			}
		}
		_gui_manager.selected_tiles = result;
		return result;
	}

	public static void ExpandRegions(List<List<Tile>> selection_list){
		var total_count = 0;
		foreach(var region in selection_list){
			total_count += region.Count;
		}
		var itr_count = 0;
		while(true){
			itr_count++;
			if(itr_count > 100){
				throw(new Exception("Expanding took too long"));
			}
			for(var i = 0; i < selection_list.Count; i++){
				var expanded = MapGen.Grow(selection_list[i]);
				for(var j = 0; j < expanded.Count; j++){
					var t = expanded[j];
					//If its in the sea, reject
					if(!t.land){
						expanded.Remove(t);
						j--;
					}
					//If its in another region3, reject
					var inAnotherRegion = false;
					foreach(var region in selection_list){
						if(region != selection_list[i]){
							if(region.IndexOf(t) != -1){
								inAnotherRegion = true;
							}
						}
					}
					if(inAnotherRegion){
						expanded.Remove(t);
						j--;
					}
				}
				selection_list[i] = expanded;
			}

			//Break if no change
			var new_count = 0;
			foreach(var region in selection_list){
				new_count += region.Count;
			}
			if(total_count == new_count){
				return;
			}else{
				total_count = new_count;
			}
		}
	}

	public static List<List<Tile>> SeperateContiguous(List<Tile> tiles){
		var time = Time.realtimeSinceStartup;
		List<Tile> src = new List<Tile>();
		List<List<Tile>> result = new List<List<Tile>>();
		foreach(var t in tiles){
			src.Add(t);
		}
		while(src.Count > 0){
			var region = MapGen.SelectContiguous(src[0], src);
			result.Add(region);
			foreach(var t in region){
				src.Remove(t);
			}
		}
		return result;
	}


	public static List<Tile> SelectContiguous(Tile start, List<Tile> tiles){
		var time = Time.realtimeSinceStartup;
		List<Tile> result = new List<Tile>();
		List<Tile> open = new List<Tile>();
		open.Add(start);
		var i = 0;
		while(open.Count > 0){
			i++;
			if(i > tiles.Count){
				throw(new Exception("Fill fail"));
			}
			var t = open[0];
			open.RemoveAt(0);
			result.Add(t);
			foreach(var n in t.adjacent){
				if(result.IndexOf(n) == -1 && open.IndexOf(n) == -1 && tiles.IndexOf(n) != -1){
					open.Add(n);
				}
			}
		}
		_gui_manager.selected_tiles = result;
		return result;
	}

	private class SearchRecord{
    public Tile t;
    public int cost;
  }
	public static Region ClosestRegion(List<Tile> tiles, Region to_ignore = null){
		var open = new List<SearchRecord>();
		var openTiles = new List<Tile>();
		var closed = new List<Tile>();
		foreach(var t in tiles){
			if(t.adjacent.FindAll((n)=>n.region != t.region).Count > 0){
				var record = new SearchRecord();
				record.t = t;
				record.cost = 0;
				open.Add(record);
			}else{
				closed.Add(t);
			}
		}
		var i = 0;
		while(open.Count > 0){
			i++;
			if(i > 10000){
				throw(new Exception("Finding closest Region took too long"));
			}
			var node = open[0];
			closed.Add(node.t);
			open.RemoveAt(0);
			openTiles.Remove(node.t);
			if(node.t.region != null && node.t.region != to_ignore){
				return node.t.region;
			}
			foreach(var n in node.t.adjacent){
				if(closed.IndexOf(n) == -1 && openTiles.IndexOf(n) == -1){
					var record = new SearchRecord();
					record.t = n;
					record.cost = node.cost + 1;
					open.Add(record);
					openTiles.Add(record.t);
				}
			}
		}
		_gui_manager.selected_region = null;
		_gui_manager.selected_tiles = tiles;
		throw new Exception("Could not find any other Regions");
	}

	// Update is called once per frame
	void Update () {

	}
}
