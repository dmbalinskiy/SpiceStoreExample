using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpiceStoreExample.Models
{
    public class Subcategory
    {
        [Key]
        public int Key { get; set; }

        [Display(Name = "Category")]
        [Required]
        public int CategoryId { get; set; }

        [Display(Name = "Subcategory Name")]
        [Required]
        public string Name { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set;}
    }
}
