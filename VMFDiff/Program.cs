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

            if(!File.Exists(oldVMFFilePath) || !File.Exists(newVMFFilePath))
            {
                //exit
                return;
            }
            
            var oldVMF = new VMF(File.ReadAllLines(oldVMFFilePath));
            var newVMF = new VMF(File.ReadAllLines(newVMFFilePath));

            var oldWorld = oldVMF.Body.Where(node => node.Name == "world").Where(node => node is VBlock).Cast<VBlock>().FirstOrDefault();
            var newWorld = newVMF.Body.Where(node => node.Name == "world").Where(node => node is VBlock).Cast<VBlock>().FirstOrDefault();

            //clear visgroups
            //set mapversion to 1
            //iterate through world find differences, add to outWorld and visgroups
            //iterate through the rest of the entities and do the same
        }
    }
}

