using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Sygenap
{
    public class Parcel : MonoBehaviour
    {
        public static float WIDTH = 10f;

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

        /*
         * POV SYSTEM
         */

        public void display()
        {
            StartCoroutine(this.r_display());
        }
        private IEnumerator r_display()
        {
            if (File.Exists(this.getSaveFileURI()))
            {
                this._status = STATUS.DISPLAYING;
                yield return null;
            }
            else
            {
                this._status = STATUS.GENERATING;
                yield return null;
            }

            this._status = STATUS.DISPLAYED;
        }

        public void hide()
        {
            StartCoroutine(this.r_hide());
        }
        private IEnumerator r_hide()
        {
            this._status = STATUS.HIDING;
            yield return null;
        }

        /*
         * SAVING SYSTEM
         */

        public string getSaveFileURI()
        {
            return this.root.getSaveFileURI() + "-" + this._x + "-" + this._y;
        }

        public void save()
        {
            string destination = this.getSaveFileURI();
            FileStream file;

            if (File.Exists(destination)) file = File.OpenWrite(destination);
            else file = File.Create(destination);

            ParcelData data = new ParcelData(this._x, this._y);

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
        }
    }

    [System.Serializable]
    public class ParcelData
    {
        public int x;
        public int y;

        public ParcelData(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }
}