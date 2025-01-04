using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Sygenap
{
    public class Parcel : MonoBehaviour
    {
        public Sygenap root;

        private int _x; //Coordinate in X times WIDTH
        public int x { get { return _x; } }

        private int _y; //Coordinate in Y times WIDTH
        public int y { get { return _y; } }

        public enum STATUS
        {
            WAITING_FOR_GENERATION,
            GENERATING,
            WAITING_FOR_DISPLAY,
            DISPLAYING,
            DISPLAYED,
            HIDING
        }
        private STATUS _status = STATUS.DISPLAYED;
        public STATUS status { get { return _status; } }

        public static float GENERATION_BUFFER_FREQUENCY_CHECK = 2f;
        private Coroutine generateAndOrDisplayProcess;

        private Terrain _terrain;
        private float[,] _terrainHeights;
        private TerrainCollider _collider;

        public List<Decor> decors = new List<Decor>();

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
                this._x * this.root.PARCEL_WIDTH,
                0f,
                this._y * this.root.PARCEL_WIDTH
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
            if (this.generateAndOrDisplayProcess != null)
                return;

            this.generateAndOrDisplayProcess = StartCoroutine(this.r_startGenerateAndOrDisplay());
        }
        private IEnumerator r_startGenerateAndOrDisplay()
        {
            if (!File.Exists(this.getSaveFileURI()))
            {
                this._status = STATUS.WAITING_FOR_GENERATION;
                this.logStatus();
                yield return StartCoroutine(this.r_generate());

                this._status = STATUS.WAITING_FOR_DISPLAY;
                this.logStatus();
                yield return StartCoroutine(this.r_display());

                this._status = STATUS.DISPLAYED;
                this.logStatus();

                if(this.root.saveParcelsOnGeneration)
                    this.save();
            }
            else
            {
                this.load();

                this._status = STATUS.WAITING_FOR_DISPLAY;
                this.logStatus();
                yield return StartCoroutine(this.r_display());

                this._status = STATUS.DISPLAYED;
                this.logStatus();
            }

            this.generateAndOrDisplayProcess = null;
        }

        private IEnumerator r_display()
        {
            //TODO display buffering
            this._status = STATUS.DISPLAYING;

            if (this.root.shouldGenerateTerrain)
            {
                this._terrain = this.gameObject.AddComponent<Terrain>();
                this._terrain.materialTemplate = this.root.terrainMaterial;
                this._terrain.materialType = Terrain.MaterialType.Custom;

                _terrain.terrainData = new TerrainData();
                _terrain.terrainData.size = new Vector3(this.root.PARCEL_WIDTH, this.root.PARCEL_MAX_HEIGHT, this.root.PARCEL_WIDTH);
                _terrain.terrainData.SetHeights(0, 0, this._terrainHeights);

                this._collider = this.gameObject.AddComponent<TerrainCollider>();
                this._collider.terrainData = _terrain.terrainData;
            }

            yield return null;
        }

        private IEnumerator r_generate()
        {
            this.root.generationWaitingList.Add(this);
            while (!this.root.generationBuffer.Contains(this))
            {
                yield return new WaitForSeconds(Parcel.GENERATION_BUFFER_FREQUENCY_CHECK);
            }
            this.root.generationWaitingList.Remove(this);

            this._status = STATUS.GENERATING;

            if (this.root.shouldGenerateTerrain)
            {
                //int heightsArrayDimension = Mathf.RoundToInt(Parcel.WIDTH * 512);
                int heightsArrayDimension = 33;
                this._terrainHeights = new float[heightsArrayDimension, heightsArrayDimension];

                //Generate noise on terrain
                float noiseGenerationPercent = 0f;
                //Gives a pseudo-random offset to every Parcels, according to the the seed
                //with reducing the seed to a value really smaller to its maximum value
                //in order to avoid memory overload on the following vector operations
                Vector2 seedPerlinCoordinatesOffset = new Vector2(
                    this.root.seed / (int.MaxValue / 5f),
                    this.root.seed / (int.MaxValue / 3f)
                );
                for (int pointX = 0; pointX < heightsArrayDimension; pointX++)
                {
                    for (int pointZ = 0; pointZ < heightsArrayDimension; pointZ++)
                    {
                        float noiseHeightValue = 0f;

                        Vector2 perlinCoordinates = new Vector2(
                            pointX / (heightsArrayDimension - 1f),
                            pointZ / (heightsArrayDimension - 1f)
                        );

                        //parcel offset in world
                        Vector2 parcelPerlinCoordinatesOffset = new Vector2(
                            this._x * 1f,
                            this._y * 1f
                        );
                        perlinCoordinates = perlinCoordinates + parcelPerlinCoordinatesOffset;

                        //offset from seed
                        perlinCoordinates = perlinCoordinates + seedPerlinCoordinatesOffset;

                        //zoom on perlin noise
                        perlinCoordinates.x *= this.root.PARCEL_TERRAIN_NOISE_PERLIN_ZOOM;
                        perlinCoordinates.y *= this.root.PARCEL_TERRAIN_NOISE_PERLIN_ZOOM;

                        noiseHeightValue = Mathf.PerlinNoise(perlinCoordinates.x, perlinCoordinates.y);

                        //using max noise height
                        noiseHeightValue *= this.root.PARCEL_TERRAIN_NOISE_MAX_HEIGHT / this.root.PARCEL_MAX_HEIGHT;

                        this._terrainHeights[pointZ, pointX] += noiseHeightValue;
                        noiseGenerationPercent = ((pointX * heightsArrayDimension) + pointZ) / (heightsArrayDimension * heightsArrayDimension * 1f);
                    }

                    //Debug.Log("Parcel " + this._x + ", " + this._y + " - Generating noise... " + (noiseGenerationPercent*100f)+"%");
                    yield return null;
                }
            }

            yield return StartCoroutine(this.r_generateDecors());
        }

        private IEnumerator r_generateDecors()
        {
            foreach (ParcelDecorRule rule in this.root.possibleDecorsForParcels)
            {
                int numberOfInstances = rule.minimumOccurence + Mathf.RoundToInt((rule.maximumOccurence - rule.minimumOccurence) * this.root.random());

                for (int i = 0; i < numberOfInstances; i++)
                {
                    GameObject newInstanceObj = Instantiate(rule.prefab);
                    Vector3 newInstancePosition;
                    if (rule.shouldBePlacedRandomly) {
                        newInstancePosition = this.getOrigin() + new Vector3(
                            this.root.PARCEL_WIDTH * this.root.random(),
                            0f,
                            this.root.PARCEL_WIDTH * this.root.random()
                        );
                    }
                    else
                    {
                        newInstancePosition = this.getOrigin();
                    }
                    newInstanceObj.transform.position = newInstancePosition;
                    newInstanceObj.transform.SetParent(this.transform);

                    Decor newDecor = newInstanceObj.AddComponent<Decor>();
                    this.addDecor(newDecor);
                }

                yield return null;
            }
        }

        public void addDecor(Decor decor)
        {
            decor.root = this.root;
            decor.parent = this;

            this.decors.Add(decor);
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

            ParcelData data = new ParcelData(this._x, this._y, this._terrainHeights, this.decors);

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

            //TODO unserialize DecorData to Decors
        }
    }

    [System.Serializable]
    public class ParcelDecorRule
    {
        public GameObject prefab;

        public int minimumOccurence = 1;
        public int maximumOccurence = 1;

        public bool shouldBePlacedRandomly = true;
    }

    [System.Serializable]
    public class ParcelData
    {
        public int x;
        public int y;

        public float[,] heights;

        public List<DecorData> decors;

        public ParcelData(int x, int y, float[,] heights, List<Decor> boundDecors) {
            this.x = x;
            this.y = y;

            this.heights = heights;

            this.decors = new List<DecorData> ();
            foreach (Decor boundDecor in boundDecors)
            {
                this.decors.Add(boundDecor.serialize());
            }
        }
    }
}