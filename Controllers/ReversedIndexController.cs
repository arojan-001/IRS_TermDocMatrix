using Microsoft.AspNetCore.Mvc;
using SearchEngineWidthReversedIndex.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SearchEngineWidthReversedIndex.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReversedIndexController : ControllerBase
    {
        private static List<Document> documents = new List<Document>
    {
        new Document { Id = 1, Text = "Call me when you get home" },
        new Document { Id = 2, Text = "He is forever complaining about this country" },
        new Document { Id = 3, Text = "If you cannot make it, call ME as soon as possible" },
        new Document { Id = 4, Text = "These are a few frequently asked questions about online courses." }
    };     
            
        private static Reversed reversed = new Reversed();

        private Reversed CreateReversedIndex()
        {
            var index = new Reversed { Index = new Dictionary<string, List<int>>() };

            foreach (var document in documents)
            {
                var words = document.Text.Split(new[] { ' ', '.', '!', '?', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var normalizedWord = word.ToLower();

                    if (index.Index.ContainsKey(normalizedWord))
                    {
                        if (!index.Index[normalizedWord].Contains(document.Id))
                        {
                            index.Index[normalizedWord].Add(document.Id);
                        }
                    }
                    else
                    {
                        index.Index[normalizedWord] = new List<int> { document.Id };
                    }
                }
            }

            return index;
        }

        [HttpPost("addDoc")]
        public ActionResult<IEnumerable<string>> AddDoc(string doc)
        {
            documents.Add(new Document {Id = (documents.Count() + 1), Text = doc });

            return Ok(documents);
        }

        [HttpGet("getMatrix")]
        public IActionResult GetTermDocMatrix()
        {
            reversed.Index = CreateReversedIndex().Index
            .OrderBy(entry => entry.Key)  // Sort by the key (term)
            .ToDictionary(entry => entry.Key, entry => entry.Value);

            var retval = new List<KeyValuePair<string, int>>();
            foreach (var reversedvalue in reversed.Index)
            {
                foreach (var doc in reversed.Index[reversedvalue.Key])
                    retval.Add(new KeyValuePair<string, int>(reversedvalue.Key, doc));
            }
            return Ok(retval);
        }

        [HttpGet("getDoc")]
        public IActionResult GetDoc(int docId)
        {
            return Ok(documents.Where(doc => doc.Id == docId));
        }

        [HttpGet("searchDocIdtxt")]
        public IActionResult SearchDocIdtxt(string srchtxt)
        {
            if (string.IsNullOrEmpty(srchtxt))
                return BadRequest("Search text cannot be empty.");

            var srchtxtKey = srchtxt.ToLower().Split(new[] { ' ', '.', '!', '?', ',' }, StringSplitOptions.RemoveEmptyEntries);

            List<int> result = null;

            foreach (var srch in srchtxtKey)
            {
                if (reversed.Index.ContainsKey(srch))
                {
                    var docs = reversed.Index[srch];

                    if (result == null)
                    {
                        // If result is null, initialize it with the document IDs from the first term
                        result = new List<int>(docs);
                    }
                    else
                    {
                        // Find the intersection of the current result and the document IDs for the current term
                        result = Intersect<int>(result, docs);
                        if (result.Count() == 0){
                            // If a term is not found, return a bad request with the missing term
                            return BadRequest($"No occurrences of '{srchtxt}' found.");
                        }
                    }
                }
                else
                {
                    // If a term is not found, return a bad request with the missing term
                    return BadRequest($"No occurrences of '{srchtxt}' found.");
                }
            }

            return Ok(result);
        }
        public static List<T> Intersect<T>(List<T> list1, List<T> list2)
        {
            var set = new HashSet<T>(list2);
            var result = new List<T>();

            foreach (var item in list1)
                if (set.Contains(item))
                    result.Add(item);

            return result;
        }


    }

}
