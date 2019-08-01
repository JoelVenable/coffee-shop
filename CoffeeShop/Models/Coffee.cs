using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using System.Threading.Tasks;

namespace CoffeeShop.Models
{
    public class Coffee
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Title { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "BeanType is too long!")]
        public string BeanType { get; set; }
    }
}
