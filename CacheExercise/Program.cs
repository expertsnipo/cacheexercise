using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheExercise
{
    class Set
    {
        bool cachehit = false, blockread = false, blockwritten = false;
        int linenumber = 0, blocknumberread, blocknumberwritten;

        Line[] lines;

        public Set(int numLines, int blockSize)
        {
            lines = new Line[numLines];
            for (int i = 0; i < numLines; ++i)
            {
                Line line = new Line(blockSize, i);
                lines[i] = line;
            }

        }
        public void resetStats()
        {
            cachehit = false;
            blockread = false;
            blockwritten = false;
            linenumber = 0;
            blocknumberread = 0;
            blocknumberwritten = 0;
        }

        public bool lookup(bool write, short newtag)
        {
            bool tagMatch = false;
            foreach (Line l in lines)
            {
                if (l.getTag() == newtag)
                {
                    if (l.isValid())
                    {
                        // cache hit
                        tagMatch = true;
                        if (write)
                        {
                            l.write();
                        }
                        return true;
                    }
                    else
                    {
                        // cache miss
                        // load block into cache
                        // find invalid line and load
                        bool needToEvict = true;
                        for (int j = 0; j < 2; ++j)
                        {
                            if (!lines[j].isValid())
                            {
                                needToEvict = false;
                                blockread = true;

                                lines[j].read(newtag);
                                linenumber = j;
                                blocknumberread = lines[j].getBlockNumber();
                            }
                        }
                        if (needToEvict)
                        {
                            // find least-recently used line and evict
                            DateTime firsttime;
                            int winner = 0;
                            firsttime = lines[0].getLastUsed();
                            for (int i = 1; i < lines.Length; ++i)
                            {
                                if (lines[i].getLastUsed() < firsttime)
                                {
                                    winner = i;
                                    firsttime = lines[i].getLastUsed();
                                }

                            }

                            if (write)
                            {
                                if (lines[winner].isDirty())
                                {
                                    blockwritten = true;
                                    blocknumberwritten = lines[winner].getBlockNumber();
                                }
                            }
                            lines[winner].read(newtag);
                            blockread = true;
                            blocknumberread = lines[winner].getBlockNumber();
                            linenumber = winner;
                        }

                        return false;
                    }
                }
            }
            if (!tagMatch)
            {
                // cache miss
                // load block into cache
                bool needToEvict = true;
                for (int i = 0; i < 2; ++i)
                {
                    if (!lines[i].isValid())
                    {
                        needToEvict = false;
                        lines[i].read(newtag);
                        blockread = true;
                        linenumber = i;
                        blocknumberread = lines[i].getBlockNumber();
                        break;
                    }
                }
                if (needToEvict)
                {
                    // find least-recently used line and evict
                    DateTime firsttime;
                    int winner = 0;
                    firsttime = lines[0].getLastUsed();
                    for (int i = 1; i < lines.Length; ++i)
                    {
                        if (lines[i].getLastUsed() < firsttime)
                        {
                            winner = i;
                            firsttime = lines[i].getLastUsed();
                        }

                    }

                    if (write)
                    {
                        if (lines[winner].isDirty())
                        {
                            blockwritten = true;
                            blocknumberwritten = lines[winner].getBlockNumber();
                        }

                    }
                    lines[winner].read(newtag);
                    blockread = true;
                    linenumber = winner;
                    blocknumberread = lines[winner].getBlockNumber();
                }
            }
            return false;
        }

        public bool getBlockRead() { return blockread; }
        public bool getBlockWritten() { return blockwritten; }
        public int getLineNumber() { return linenumber; }
        public int getBlockNumberRead() { return blocknumberread; }
        public int getBlockNumberWritten() { return blocknumberwritten; }
    }

    class Line
    {
        byte[] block;
        int blocknumber;
        bool valid;
        bool dirty;
        // valid should mean not empty
        //bool empty; 
        short tag;
        DateTime lastUsed;

        public Line(int blockSize, int newblocknumber)
        {
            block = new byte[blockSize];
            valid = false;
            dirty = false;
            //empty = true;
            tag = 0;
            blocknumber = newblocknumber;
        }

        public short getTag() { return tag; }
        public bool isValid() { return valid; }
        public bool isDirty() { return dirty; }
        public int getBlockNumber() { return blocknumber; }
        //public bool isEmpty() { return empty; }

        // Load byte from memory into cache
        public void read(short newtag)
        {
            //empty = false;
            valid = true;
            lastUsed = DateTime.Now;
            tag = newtag;
            dirty = false;
        }

        // Write byte into memory from cache
        public void write()
        {
            dirty = true;
        }
        public DateTime getLastUsed() { return lastUsed; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            int S = 4, E = 2, B = 64;
            int s, b, t;
            int m = 16;
            byte tbits, sbits, bbits;

            if (args.Length > 2)
            {
                S = Convert.ToInt32(args[0]);
                E = Convert.ToInt32(args[1]);
                B = Convert.ToInt32(args[2]);
            }

            bool cachehit = false, blockread = false, blockwritten = false;
            int blocknumberread = 0, setnumber = 0, linenumber = 0, blocknumberwritten = 0;

            string[] input = new string[100];
            string line;
            int j = 0;
            while ((line = Console.ReadLine()) != null)
            {
                if (j < 100)
                {
                    input[j] = line;
                    ++j;
                }
                else
                {
                    Console.WriteLine("Too much input!");
                    Environment.Exit(1);
                }
            }


            s = (int)Math.Log(S, 2);
            b = (int)Math.Log(B, 2);
            t = m - (s + b);

            // BUILD THE CACHE
            // need data size larger than byte for valid bit and tag bits            
            //short[,] addrcache = new short[4,2];
            // byte[numSets, numLines, numBytes in block]
            //byte[,,] cache = new byte[4, 2, 64];

            //byte[] mem = new byte[65536];
            Set[] cache = new Set[S];

            for (int i = 0; i < S; ++i)
            {
                Set set = new Set(E, B);
                cache[i] = set;
            }

            for (int i = 0; i < 100; ++i)
            {
                if (null == input[i])
                {
                    break;
                }
                // main loop
                string[] curline = input[i].Split(' ');
                string OP = "";
                short addr = 0;
                if (curline.Length > 1)
                {
                    OP = curline[0];
                    addr = (short)Convert.ToInt32(curline[1]);
                }
                else
                {
                    // input invalid
                    Console.WriteLine("Invalid input!");
                    Environment.Exit(1);
                }
                

                bool write = false; ;
                switch (OP)
                {
                    case "read":
                        write = false;
                        break;
                    case "write":
                        write = true;
                        break;
                    default:
                        //error
                        Console.WriteLine("Invalid OP!");
                        Environment.Exit(1);
                        break;
                }


                // will change based on t, s, and b
                // need to figure out how to make dynamic masks
                // tbits = shift by (s+b)
                tbits = (byte)(addr >> (s+b));
                bbits = (byte)((addr & (B - 1)));
                sbits = (byte)((addr >> b) & (S-1));

                // convert addr into block number
                int blocknumber = addr / B;

                // get current set
                Set curSet = cache[sbits];
                // check the valid bits and tag bits in the selected set
                // ref bool blockread, ref int blocknumberread, ref int linenumber, ref bool blockwritten, ref int blocknumberwritten
                cachehit = curSet.lookup(write, tbits);
                blockread = curSet.getBlockRead();
                blockwritten = curSet.getBlockWritten();
                setnumber = sbits;
                //blocknumberread = curSet.getBlockNumberRead();
                //blocknumberwritten = curSet.getBlockNumberWritten();

                Console.WriteLine(String.Format("{0} {1} {2} {3} {4} {5} {6}", cachehit ? 1 : 0, blockread ? 1 : 0, blockread ? blocknumber.ToString() : "-", blockread ? setnumber.ToString() : "-", blockread ? linenumber.ToString() : "-", blockwritten ? 1 : 0, blockwritten ? blocknumber.ToString() : "-"));
                curSet.resetStats();

            }
        }

    }
}
