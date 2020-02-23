using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceStoreExample.Models.ViewModels
{
    public class SubcategoryAndCategoryViewModel
    {
        public IEnumerable<Category> CategoryList { get; set; }
        public Subcategory Subcategory { get; set; }
        public List<string> SubcategoryList { get; set; }
        public string StatusMessage { get; set; }

    }
}
