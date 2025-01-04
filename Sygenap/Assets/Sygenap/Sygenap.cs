using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Sygenap {
    public class Sygenap : MonoBehaviour
    {
        private System.Random r;

        public string gameName;

        private int _seed = 1;
        public int seed
        {
            get
            {
                return _seed;
            }
        }

        public int startingSeed = 1;
        public bool randomSeed = true;

        public bool saveParcelsOnGeneration = true;

        public float PARCEL_WIDTH = 10f;
        public float PARCEL_MAX_HEIGHT = 300f;
        public float PARCEL_TERRAIN_NOISE_PERLIN_ZOOM = 0.03f;
        public float PARCEL_TERRAIN_NOISE_MAX_HEIGHT = 3f; //Noise height, in meters
        public List<Parcel> parcels = new List<Parcel>();

        public int GENERATION_BUFFER_SIZE = 3;
        public float GENERATION_BUFFER_REFRESH_FREQUENCY = 1f; //In seconds
        public List<Parcel> generationWaitingList = new List<Parcel>();
        public List<Parcel> generationBuffer = new List<Parcel>();

        public float POV_REACH = 50f; //Limit inside which Parcels are displayed (and generated if necessary) and outside which Parcels are hidden. In meters.
        public float POV_REFRESH_FREQUENCY = 1f; //In seconds
        private POV _pov;
        public POV pov
        {
            get { return _pov; }
        }
        public GameObject povPrefab;

        public Material terrainMaterial;

        public List<ParcelDecorRule> possibleDecorsForParcels = new List<ParcelDecorRule>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Debug.Log("======");
            Debug.Log("SYGENAP");
            Debug.Log("Current game: "+this.getSaveFileURI());
            Debug.Log("======");

            if (File.Exists(this.getSaveFileURI()))
            {
                Debug.Log("Loading level... "+this.getSaveFileDirectory());
                this.loadLevel();
            }
            else
            {
                Debug.Log("Creating level... " + this.getSaveFileDirectory());
                Directory.CreateDirectory(this.getSaveFileDirectory());

                if (this.randomSeed)
                    this._seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                else
                    this._seed = this.startingSeed;

                this.spawnPOV(Vector3.zero, Quaternion.identity);
            }

            this.r = new System.Random(this._seed);

            StartCoroutine(this.r_handlePov());
            StartCoroutine(this.r_handleGenerationBuffer());
        }

        // Update is called once per frame
        void Update()
        {

        }

        public Parcel getParcelAt(Vector2Int coordinates)
        {
            foreach (Parcel parcel in this.parcels)
            {
                if(parcel.x == coordinates.x && parcel.y == coordinates.y)
                {
                    return parcel;
                }
            }
            return null;
        }

        public Parcel getParcelAt(Vector3 position)
        {
            foreach (Parcel parcel in this.parcels)
            {
                Vector3 origin = parcel.getOrigin();

                if (
                    origin.x < position.x && position.x <= origin.x + this.PARCEL_WIDTH
                    && origin.z < position.z && position.z <= origin.z + this.PARCEL_WIDTH
                )
                {
                    return parcel;
                }
            }
            return null;
        }

        /*
         * Gives a pseudo-random value between 0f and 1f
         * Always gives the same value with the same seed
         */
        public float random()
        {
            return r.Next(0, 10000)/10000f;
        }

        /*
         * POV SYSTEM
         */

        public Vector2Int getPovCoordinates()
        {
            return new Vector2Int(
                Mathf.FloorToInt(this.pov.transform.position.x / this.PARCEL_WIDTH),
                Mathf.FloorToInt(this.pov.transform.position.z / this.PARCEL_WIDTH)
            );
        }

        private IEnumerator r_handlePov()
        {
            bool continued = true;
            while (continued)
            {
                Vector2Int povCoordinates = this.getPovCoordinates();

                List<Parcel> shouldStayDisplayed = new List<Parcel>();

                int reachInParcels = Mathf.CeilToInt(this.POV_REACH / this.PARCEL_WIDTH);
                for(int x = povCoordinates.x - reachInParcels; x < povCoordinates.x + reachInParcels; x++)
                {
                    for (int y = povCoordinates.y - reachInParcels; y < povCoordinates.y + reachInParcels; y++)
                    {
                        Parcel parcelInReach = this.getParcelAt(new Vector2Int(x, y));
                        
                        if (parcelInReach != null)
                        {

                        }
                        else
                        {
                            parcelInReach = this.displayParcelAt(x, y);
                            parcelInReach.display();
                        }

                        shouldStayDisplayed.Add(parcelInReach);
                        yield return null;
                    }
                }

                List<Parcel> shouldHide = new List<Parcel>();
                foreach (Parcel parcel in this.parcels)
                {
                    if (parcel.status != Parcel.STATUS.HIDING && !shouldStayDisplayed.Contains(parcel))
                    {
                        shouldHide.Add(parcel);
                    }
                }

                foreach (Parcel parcel in shouldHide)
                {
                    parcel.hide();
                }

                yield return new WaitForSeconds(this.POV_REFRESH_FREQUENCY);
            }
        }

        private Parcel displayParcelAt(int x, int y)
        {
            GameObject parcelObj = new GameObject("Parcel"+x+"-"+y);
            parcelObj.transform.position = new Vector3(x*this.PARCEL_WIDTH, 0f, y*this.PARCEL_WIDTH);

            Parcel parcel = parcelObj.AddComponent<Parcel>();
            parcel.init(this, x, y);

            this.parcels.Add(parcel);
            return parcel;
        }

        private IEnumerator r_handleGenerationBuffer()
        {
            bool continued = true;
            while (continued)
            {
                if (this.generationBuffer.Count > 0)
                {
                    //Clean generation buffer by removing already generated Parcels
                    foreach(Parcel parcel in this.generationBuffer.ToArray())
                    {
                        if (parcel.status != Parcel.STATUS.WAITING_FOR_GENERATION && parcel.status != Parcel.STATUS.GENERATING)
                            this.generationBuffer.Remove(parcel);
                    }
                }

                if (this.generationWaitingList.Count > 0)
                {
                    //TODO decide which Parcels to generate first according to specific criteria (like distance, game design importance, etc.)
                    int maxNumberOfParcelToAdd = this.GENERATION_BUFFER_SIZE - this.generationBuffer.Count;
                    if (maxNumberOfParcelToAdd > this.generationWaitingList.Count)
                        maxNumberOfParcelToAdd = this.generationWaitingList.Count;
                    List<Parcel> parcelsToGenerate = this.generationWaitingList.GetRange(0, maxNumberOfParcelToAdd);
                    this.generationBuffer.AddRange(parcelsToGenerate);
                }

                yield return new WaitForSeconds(this.GENERATION_BUFFER_REFRESH_FREQUENCY);
            }
        }

        /*
         * SAVING SYSTEM
         */


        public string getSaveFileDirectory()
        {
            return Application.persistentDataPath + "/" + this.gameName + "/";
        }
        
        public string getSaveFileURI()
        {
            return Application.persistentDataPath + "/" + this.gameName + "/." + this._seed;
        }

        public void saveLevel()
        {
            string destination = this.getSaveFileURI();
            FileStream file;

            if (File.Exists(destination)) file = File.OpenWrite(destination);
            else file = File.Create(destination);

            SygenapData data = new SygenapData(this._seed, this._pov.transform.position, this._pov.transform.localRotation);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, data);
            file.Close();
        }

        private void loadLevel()
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
            SygenapData data = (SygenapData)bf.Deserialize(file);
            file.Close();

            this.spawnPOV(data.povPosition, data.povRotation);
        }

        private void spawnPOV(Vector3 position, Quaternion rotation)
        {
            GameObject povObj = Instantiate(this.povPrefab);
            povObj.transform.position = position;
            povObj.transform.localRotation = rotation;

            this._pov = povObj.AddComponent<POV>();
        }
    }

    [System.Serializable]
    public class SygenapData{
        public int seed;
        public Vector3 povPosition;
        public Quaternion povRotation;

        public SygenapData(int seed, Vector3 povPosition, Quaternion povRotation)
        {
            this.seed = seed;
            this.povPosition = povPosition;
            this.povRotation = povRotation;
        }
    }
}