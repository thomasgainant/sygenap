using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sygenap
{
    public class Parcel : MonoBehaviour
    {
        public static float WIDTH = 10f;
        public static float MAX_HEIGHT = 5f;

        public Sygenap root;

        private int _x; //Coordinate in X times WIDTH
        public int x { get { return _x; } }

        private int _y; //Coordinate in Y times WIDTH
        public int y { get { return _y; } }

        public enum STATUS
        {
            GENERATING,
            DISPLAYING,
            DISPLAYED,
            HIDING
        }
        private STATUS _status = STATUS.DISPLAYED;
        public STATUS status { get { return _status; } }

        private Terrain _terrain;
        private float[,] _terrainHeights;
        private TerrainCollider _collider;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        public void init(Sygenap root, int x, int y)
        {
            this.root = root;
            this._x = x;
            this._y = y;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public Vector3 getOrigin()
        {
            return new Vector3(
                this._x * Parcel.WIDTH,
                0f,
                this._y * Parcel.WIDTH
            );
        }

        private void logStatus()
        {
            Debug.Log("Parcel "+this._x+", "+this._y+" - "+this.status);
        }

        /*
         * POV SYSTEM
         */

        public void display()
        {
            StartCoroutine(this.r_startDisplay());
        }
        private IEnumerator r_startDisplay()
        {
            if (!File.Exists(this.getSaveFileURI()))
            {
                this._status = STATUS.GENERATING;
                this.logStatus();
                yield return StartCoroutine(this.r_generate());

                this._status = STATUS.DISPLAYING;
                this.logStatus();
                yield return StartCoroutine(this.r_display());

                this._status = STATUS.DISPLAYED;
                this.logStatus();

                this.save();
            }
            else
            {
                this.load();

                this._status = STATUS.DISPLAYING;
                this.logStatus();
                yield return StartCoroutine(this.r_display());

                this._status = STATUS.DISPLAYED;
                this.logStatus();
            }
        }

        private IEnumerator r_display()
        {
            this._terrain = this.gameObject.AddComponent<Terrain>();
            this._terrain.materialTemplate = this.root.terrainMaterial;
            this._terrain.materialType = Terrain.MaterialType.Custom;

            _terrain.terrainData = new TerrainData();
            _terrain.terrainData.size = new Vector3(Parcel.WIDTH, Parcel.MAX_HEIGHT, Parcel.WIDTH);
            _terrain.terrainData.SetHeights(0, 0, this._terrainHeights);

            this._collider = this.gameObject.AddComponent<TerrainCollider>();
            this._collider.terrainData = _terrain.terrainData;

            yield return null;
        }

        private IEnumerator r_generate()
        {
            //int heightsArrayDimension = Mathf.RoundToInt(Parcel.WIDTH * 512);
            int heightsArrayDimension = 33;
            this._terrainHeights = new float[heightsArrayDimension, heightsArrayDimension];

            //Generate noise on terrain
            float noiseGenerationPercent = 0f;
            for (int x = 0; x < heightsArrayDimension; x++)
            {
                for (int z = 0; z < heightsArrayDimension; z++)
                {
                    float heightValue = 0f;

                    heightValue = Mathf.PerlinNoise(x / (heightsArrayDimension * 1f), z / (heightsArrayDimension * 1f)); //TODO using max noise height + parcel offset in world + zoom on perlin noise + offset from seed

                    this._terrainHeights[z, x] = heightValue;
                    noiseGenerationPercent = ((x*heightsArrayDimension)+z) / (heightsArrayDimension * heightsArrayDimension * 1f);
                }

                Debug.Log("Parcel " + this._x + ", " + this._y + " - Generating noise... " + (noiseGenerationPercent*100f)+"%");
                yield return null;
            }
        }

        public void hide()
        {
            StartCoroutine(this.r_hide());
        }
        private IEnumerator r_hide()
        {
            this._status = STATUS.HIDING;
            this.root.parcels.Remove(this);

            yield return null;

            Destroy(this.gameObject);
        }

        /*
         * SAVING SYSTEM
         */

        public string getSaveFileURI()
        {
            return this.root.getSaveFileDirectory() + this._x + "-" + this._y;
        }

        public void save()
        {
            string destination = this.getSaveFileURI();
            FileStream file;

            if (File.Exists(destination)) file = File.OpenWrite(destination);
            else file = File.Create(destination);

            ParcelData data = new ParcelData(this._x, this._y, this._terrainHeights);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, data);
            file.Close();
        }

        public void load()
        {
            string destination = this.getSaveFileURI();
            FileStream file;

            if (File.Exists(destination)) file = File.OpenRead(destination);
            else
            {
                Debug.LogError("File not found");
                return;
            }

            BinaryFormatter bf = new BinaryFormatter();
            ParcelData data = (ParcelData)bf.Deserialize(file);
            file.Close();

            this._x = data.x;
            this._y = data.y;
            this._terrainHeights = data.heights;
        }
    }

    [System.Serializable]
    public class ParcelData
    {
        public int x;
        public int y;

        public float[,] heights;

        public ParcelData(int x, int y, float[,] heights) {
            this.x = x;
            this.y = y;

            this.heights = heights;
        }
    }
}