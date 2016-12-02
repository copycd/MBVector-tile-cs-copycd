﻿using Mapbox.VectorTile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Bench
{
    class Program
    {
        static int Main(string[] args)
        {

            //ul 14/4680/6260
            //lr 14/4693/6274
            ulong zoom = 14;
            ulong minCol = 4680;
            ulong minRow = 6260;
            ulong maxCol = 4693;
            ulong maxRow = 6274;

            string fixturePath = Path.Combine("..", "bench", "mvt-bench-fixtures", "fixtures");
            if (!Directory.Exists(fixturePath))
            {
                Console.Error.WriteLine("fixture directory not found: [{0}]", fixturePath);
                return 1;
            }

            ulong nrOfTiles = (maxCol - minCol + 1) * (maxRow - minRow + 1);
            List<TileData> tiles = new List<TileData>((int)nrOfTiles);

            for (ulong col = minCol; col <= maxCol; col++)
            {
                for (ulong row = minRow; row <= maxRow; row++)
                {
                    string fileName = string.Format("{0}-{1}-{2}.mvt", zoom, col, row);
                    fileName = Path.Combine(fixturePath, fileName);
                    if (!File.Exists(fileName))
                    {
                        Console.Error.WriteLine("fixture mvt not found: [{0}]", fileName);
                        return 1;
                    }
                    else
                    {
                        tiles.Add(new TileData()
                        {
                            zoom = zoom,
                            col = col,
                            row = row,
                            pbf = File.ReadAllBytes(fileName)
                        });
                    }
                }
            }

            Stopwatch stopWatch = new Stopwatch();
            List<long> elapsed = new List<long>();

            for (int i = 0; i <= 100; i++)
            {
                Console.Write(".");
                stopWatch.Start();
                foreach (var tile in tiles)
                {
                    VectorTile vt = new VectorTile(tile.pbf);
                    foreach (var layerName in vt.LayerNames())
                    {
                        VectorTileLayer layer = vt.GetLayer(layerName);
                        for (int j = 0; j < layer.FeatureCount(); j++)
                        {
                            VectorTileFeature feat = layer.GetFeature(j);
                            var props = feat.GetProperties();
                        }
                    }
                }
                stopWatch.Stop();
                //skip first run
                if (i != 0)
                {
                    elapsed.Add(stopWatch.ElapsedMilliseconds);
                }
                stopWatch.Reset();
            }


            Console.WriteLine(
@"
runs          : {0}
tiles per run : {1}
min [ms]      : {2}
max [ms]      : {3}
avg [ms]      : {4}
StdDev        : {5:0.00}
overall [ms]  : {6}
tiles/sec     : {7:0.0}
",
               elapsed.Count,
               tiles.Count,
               elapsed.Min(),
               elapsed.Max(),
               elapsed.Average(),
               StdDev(elapsed),
               elapsed.Sum(),
               ((float)elapsed.Count * (float)tiles.Count / (float)elapsed.Sum()) * 1000
               );


            return 0;
        }


        private static double StdDev(List<long> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }


    }


    public struct TileData
    {
        public ulong zoom;
        public ulong col;
        public ulong row;
        public byte[] pbf;
    }


    public class GZipWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }
    }

}
