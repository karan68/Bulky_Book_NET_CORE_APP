using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BulkyBook.Models
{

    public class ShoppingCart
    {

        public ShoppingCart()
        {
            Count = 1; // to keep the value to 1
        }
        [Key]
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } //who is buying
        [ForeignKey("ApplicationId")]
        public ApplicationUser ApplicationUser { get; set; }

        public int ProductId { get; set; } //what  is buying
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Range(1, 1000, ErrorMessage = "Please enter a value between 1 to 1000")]
        public int Count { get; set; } //how many is buying

        [NotMapped]
        public double Price { get; set; }

    }
}
