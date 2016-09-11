using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using VMFParser;
using System.IO;

namespace VMFDiff
{
    class Program
    {
        static void Main(string[] args)
        {
            var oldVMFFilePath = string.Empty;
            var newVMFFilePath = string.Empty;

            var p = new OptionSet() {
                { "o|old=", v => { oldVMFFilePath = v; } }
               ,{ "n|new=", v => { newVMFFilePath = v; } }
            };
            var extra = p.Parse(args);

#if DEBUG
            oldVMFFilePath = "../../Test/test_2p.vmf";
            newVMFFilePath = "../../Test/test_2p_alt.vmf";
#endif

            if (!File.Exists(oldVMFFilePath) || !File.Exists(newVMFFilePath))
            {
                //exit
                return;
            }

            var oldVMF = new VMF(File.ReadAllLines(oldVMFFilePath));
            var newVMF = new VMF(File.ReadAllLines(newVMFFilePath));
            var outVMF = new VMF();

            var oldWorld = oldVMF.Body.Where(node => node.Name == "world").Where(node => node is VBlock).Cast<VBlock>().FirstOrDefault();
            var newWorld = newVMF.Body.Where(node => node.Name == "world").Where(node => node is VBlock).Cast<VBlock>().FirstOrDefault();

            //should totally use a dictionary instead
            var oldEntitiesList = oldVMF.Body.Where(node => node.Name == "entity").Where(node => node is VBlock).Cast<VBlock>().Select(block => Tuple.Create(block.Body.Where(property => property.Name == "id" && property is VProperty).Cast<VProperty>().FirstOrDefault()?.Value ?? "0", block)).ToList();
            var newEntitiesList = newVMF.Body.Where(node => node.Name == "entity").Where(node => node is VBlock).Cast<VBlock>().Select(block => Tuple.Create(block.Body.Where(property => property.Name == "id" && property is VProperty).Cast<VProperty>().FirstOrDefault()?.Value ?? "0", block)).ToList();

            //copy oldVMF
            outVMF = new VMF(oldVMF.ToVMFStrings());
            //remove all the entities
            outVMF = new VMF(outVMF.Body.Where(node => node.Name != "entity").ToList());
            //find the world
            var outWorldIndex = outVMF.Body.IndexOf(outVMF.Body.Where(node => node.Name == "world").Where(node => node is VBlock).Cast<VBlock>().FirstOrDefault());
            //make a new one
            var outNewWorld = (outVMF.Body[outWorldIndex] as VBlock).Body.Where(node => node.Name != "solid").ToList();
            //swap it in
            outVMF.Body[outWorldIndex] = new VBlock("world", outNewWorld);

            //don't include any duplicate IDs in the diffing process
            var entitiesNewDuplicateID = newEntitiesList.Where(newEntityTuple1 => newEntitiesList.Where(newEntityTuple2 => newEntityTuple1.Item1 == newEntityTuple2.Item1).Count() > 1).ToList();
            var entitiesOldDuplicateID = oldEntitiesList.Where(oldEntityTuple1 => oldEntitiesList.Where(oldEntityTuple2 => oldEntityTuple1.Item1 == oldEntityTuple2.Item1).Count() > 1).ToList();
            newEntitiesList.RemoveAll(entityTuple => entitiesNewDuplicateID.Where(duplicate => duplicate == entityTuple).Count() > 0);
            oldEntitiesList.RemoveAll(entityTuple => entitiesOldDuplicateID.Where(duplicate => duplicate == entityTuple).Count() > 0);

            var entitiesRemoved = oldEntitiesList.Where(oldEntityTuple => newEntitiesList.Where(newEntityTuple => newEntityTuple.Item1 == oldEntityTuple.Item1).Count() == 0).ToList();
            var entitiesAdded = newEntitiesList.Where(newEntityTuple => oldEntitiesList.Where(oldEntityTuple => newEntityTuple.Item1 == oldEntityTuple.Item1).Count() == 0).ToList();
            var entitiesOldRemaining = oldEntitiesList.Where(oldEntityTuple => newEntitiesList.Where(newEntityTuple => newEntityTuple.Item1 == oldEntityTuple.Item1).Count() > 0).ToList();
            var entitiesNewRemaining = newEntitiesList.Where(newEntityTuple => oldEntitiesList.Where(oldEntityTuple => newEntityTuple.Item1 == oldEntityTuple.Item1).Count() > 0).ToList();

            var entitiesNoChange = new List<Tuple<string, VBlock>>();
            var entitiesOldChange = new List<Tuple<string, VBlock>>();
            var entitiesNewChange = new List<Tuple<string, VBlock>>();
            foreach (var oldEntityTuple in entitiesOldRemaining)
            {
                var newEntityTuple = entitiesNewRemaining.Where(newEntTuple => newEntTuple.Item1 == oldEntityTuple.Item1).FirstOrDefault();

                if (VBlockMatches(oldEntityTuple.Item2, newEntityTuple.Item2))
                {
                    entitiesNoChange.Add(oldEntityTuple);
                }
                else
                {
                    entitiesOldChange.Add(oldEntityTuple);
                    entitiesNewChange.Add(newEntityTuple);
                }
            }
            
            //TODO: Include solids in the above diff checking, and separate them out when generating final VMF

            //set mapversion to 1
            //iterate through world find differences, add to outWorld and visgroups
            //iterate through the rest of the entities and do the same

            File.WriteAllLines("diff.vmf", outVMF.ToVMFStrings());
        }

        static bool VBlockMatches(VBlock block1, VBlock block2)
        {
            foreach (var b1Node in block1.Body)
            {
                var b2Node = block2.Body.Where(node => node.Name == b1Node.Name).FirstOrDefault();

                if (b2Node == null || block1.GetType() != block2.GetType())
                    return false;

                if ((b1Node is VProperty && (b1Node as VProperty).Value == (b2Node as VProperty).Value)
                    || (b1Node is VBlock && VBlockMatches((VBlock)b1Node, (VBlock)b2Node)))
                    continue;

                return false;
            }

            return true;
        }
    }
}

