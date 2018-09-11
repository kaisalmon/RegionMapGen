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
		float tileWidth = (float)base_tile.GetComponent<Renderer>().bounds.size.x - 0.1f;
		for(var x = 0; x < width; x++){
				for(var y = 0; y < height; y++){
					var pos = this.transform.position;
					pos += new Vector3((x-width/2)*tileWidth, 0, (y-height/2)*tileWidth);
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
		var image_scale = 10;
		image_scale =  heightmap.width / this.width;
		image_scale =  Math.Max(image_scale, heightmap.height / this.height);

		/************************/ Stamp("Init and Preinit");
		for(var x = 0; x < width; x++){
				for(var y = 0; y < height; y++){
					var t = grid[x,y];
					//t.land =  heightmap.GetPixel(x*image_scale, y*image_scale).grayscale < 0.5f;
					t.land = Perlin.OctavePerlin(x*scale+x_offset,y*scale+y_offset,0,7,0.3) < land_threshold;
					t.x = x;
					t.y = y;
					grid[x,y] = t;
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

			/************************/ Stamp("Seperating into base regions");
			var selection_list = SeperateContiguous(land);

			yield return new WaitForSeconds(0);
			/************************/ Stamp("Expanding into base regions");
			selection_list = MapGen.ExpandRegions(selection_list);
			yield return new WaitForSeconds(0);

			/************************/ Stamp("Finding Islands");
			var island_tiles = new TileSelection();
			foreach(var t in this.All().tiles){
				if(!t.land){
					continue;
				}
				bool in_region = false;
				foreach(var region in selection_list){
					if(region.Contains(t)){
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



			Color[] colors = new[] {new Color(0.6f,0.1f,0.1f), new Color(0,0.6f,0), new Color(0,0,0.6f),
										new Color(0.6f,0.5f,0), new Color(1,0,1), new Color(0,1,1),
									new Color(0.6f,0.5f,1), new Color(1,0.5f,1), new Color(0,0.3f,0.7f),
								new Color(0.6f,0.5f,0.5f), new Color(0.5f,0,1), new Color(0,0.6f,0.4f)};

			/************************/ Stamp("Founding Region Objects");
			yield return new WaitForSeconds(0);
			var i = 0;
			foreach(var region_tiles in selection_list){
				i++;
				i = i % colors.Length;
				var region = new Region();
				this.regions.Add(region);
				region.color = colors[i];
				foreach(var t in region_tiles.tiles){
					region.Assign(t);
				}
			}
			for(var splitcount = 0; splitcount < 20; splitcount++){
				foreach(var too_big_region in this.regions.FindAll((r)=>r.tiles.Count() > max_size)){
					/************************/ Stamp("Spliting Region");
					yield return new WaitForSeconds(2);
					i++;
					i = i % colors.Length;
					var region = new Region();
					region.color = colors[i];
					this.regions.Add(region);
					too_big_region.Split(region);
					_gui_manager.selected_tiles = new TileSelection();
				}
			}
			_gui_manager.selected_tiles = new TileSelection();

			/************************/ Stamp("Merging in Islands");

			foreach(var too_small_region in this.regions.FindAll((r)=>r.tiles.Count() < min_size)){
				yield return new WaitForSeconds(0);
				if(too_small_region.tiles.Count() != 0){
					too_small_region.MergeInto(too_small_region.ClosestRegion());
					_gui_manager.selected_tiles = too_small_region.tiles;
				}
			}

		_gui_manager.selected_tiles = new TileSelection();

		/************************/ Stamp("Removing Edge Regions");
		foreach(var region in this.regions){
			if(region.tiles.Any((t) => t.adjacent.Count() != 4)){
				foreach(var t in region.tiles.ToList()){
					t.land = false;
					t.region = null;
				}
			}
		}
		/************************/ Stamp("Finished");
		yield return new WaitForSeconds(0);
	}

	public TileSelection All(){
		TileSelection result = new TileSelection();
		for(var x = 0; x < width; x++){
				for(var y = 0; y < height; y++){
					result.Add(grid[x,y]);
				}
		}
		return result;
	}

	public static TileSelection Shrink(TileSelection tiles){
		TileSelection result = new TileSelection();
		foreach(var t in tiles.tiles){
			bool valid = true;
			foreach(var n in t.adjacent.tiles){
				if(tiles.DoesNotContain(n)){
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

	public static TileSelection Grow(TileSelection tiles){
		var time = Time.realtimeSinceStartup;
		TileSelection result = new TileSelection();
		foreach(var t in tiles.tiles){
			result.Add(t);
			foreach(var n in t.adjacent.tiles){
				result.Add(n);
			}
		}
		_gui_manager.selected_tiles = result;
		return result;
	}

	public static List<TileSelection> ExpandRegions(List<TileSelection> selection_list){
		List<TileSelection> result = new List<TileSelection>();
		var count = 0;
		while(selection_list.Count > 0){
			count++;
			if(count>1000) throw new Exception("Expanding Regions took too long");

			for(var i = 0; i < selection_list.Count; i++){
				var expanded = Grow(selection_list[i]);
				for(var j = 0; j < expanded.Count(); j++){
					var t = expanded[j];
					//If its in the sea, reject
					if(!t.land){
						expanded.Remove(t);
						j--;
					}
					//If its in another region, reject
					var inAnotherRegion = false;
					foreach(var selection in selection_list){
						if(selection != selection_list[i]){
							if(selection.Contains(t)){
								inAnotherRegion = true;
							}
						}
					}
					foreach(var selection in result){
						if(selection.Contains(t)){
							inAnotherRegion = true;
						}
					}
					if(inAnotherRegion){
						expanded.Remove(t);
						j--;
					}
				}
				if(expanded.Count() == selection_list[i].Count()){
					result.Add(expanded);
					selection_list.RemoveAt(i);
					i--;
				}else{
					selection_list[i] = expanded;
				}
			}
		}
		return result;
	}

	public static List<TileSelection> SeperateContiguous(TileSelection tiles){
		var time = Time.realtimeSinceStartup;
		TileSelection src = new TileSelection();
		List<TileSelection> result = new List<TileSelection>();
		foreach(var t in tiles.tiles){
			src.Add(t);
		}
		while(src.Count() > 0){
			var region = MapGen.SelectContiguous(src[0], src);
			result.Add(region);
			foreach(var t in region.tiles){
				src.Remove(t);
			}
		}
		return result;
	}


	public static TileSelection SelectContiguous(Tile start, TileSelection tiles){
		var time = Time.realtimeSinceStartup;
		TileSelection result = new TileSelection();
		TileSelection open = new TileSelection();
		open.Add(start);
		var i = 0;
		while(open.Count() > 0){
			i++;
			if(i > tiles.Count()){
				throw(new Exception("Fill fail"));
			}
			var t = open[0];
			open.Remove(t);
			result.Add(t);
			foreach(var n in t.adjacent.tiles){
				if(result.DoesNotContain(n) && tiles.Contains(n)){
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
	public static Region ClosestRegion(TileSelection tiles, Region to_ignore = null, HashSet<Region> whitelist = null){
		var open = new List<SearchRecord>();
		var openTiles = new TileSelection();
		var closed = new TileSelection();
		foreach(var t in tiles.tiles){
			var record = new SearchRecord();
			record.t = t;
			record.cost = 0;
			open.Add(record);
		}
		var i = 0;
		while(open.Count > 0){
			i++;
			if(i > 1000){
				throw(new Exception("Finding closest Region took too long"));
			}
			open.Sort((a, b) => a.cost - b.cost);
			var node = open[0];
			closed.Add(node.t);
			open.RemoveAt(0);
			openTiles.Remove(node.t);
			if(node.t.region != null && node.t.region != to_ignore){
				if(whitelist == null || whitelist.Contains(node.t.region)){
					return node.t.region;
				}
			}
			foreach(var n in node.t.adjacent.tiles){
				if(closed.DoesNotContain(n) && openTiles.DoesNotContain(n)){
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
		throw new Exception("Could not find any other Regions after "+i+" iterations");
	}

	// Update is called once per frame
	void Update () {

	}
}
