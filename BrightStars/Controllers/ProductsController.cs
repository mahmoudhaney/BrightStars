using BrightStars.Data;
using BrightStars.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightStars.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // For iamges-files

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        
        /// <summary>
        /// Get all products
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetProducts(string? search, string? sortType, string? sortOrder, int pageSize = 2, int pageNumber = 1)
        {
            IQueryable<Product> prods = _context.Products.AsQueryable();

            if (string.IsNullOrWhiteSpace(search) == false)
            {
                search = search.Trim();
                prods = _context.Products.Where(e => e.Name.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(sortType) && !string.IsNullOrWhiteSpace(sortOrder))
            {
                if (sortType == "Name")
                {
                    if (sortOrder == "asc")
                        prods = prods.OrderBy(e => e.Name);
                    else if (sortOrder == "desc")
                        prods = prods.OrderByDescending(e => e.Name);
                }
                else if (sortType == "Description")
                {
                    if (sortOrder == "asc")
                        prods = prods.OrderBy(e => e.Description);
                    else if (sortOrder == "desc")
                        prods = prods.OrderByDescending(e => e.Description);
                }
            }
            prods = prods.Skip(pageSize * (pageNumber - 1)).Take(pageSize);

            return Ok(prods);
        }




        /// <summary>
        /// Get a product from database using id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}", Name ="GetById")]
        public ActionResult<Product> GetProduct(int id)
        {
            if (id == 0)
                return BadRequest();
            Product product = _context.Products.Include(c => c.Category).FirstOrDefault(x => x.Id == id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        /// <summary>
        /// Add a new product
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [HttpPost]
        public ActionResult PostProduct ([FromForm]Product product)
        {
            if (product == null)
                return BadRequest();

            if (product.ImageFile == null)
                product.ImageUrl = "\\images\\No_Image.png";
            else
            {
                string imgExtension = Path.GetExtension(product.ImageFile.FileName);

                //if (imgExtension != ".png" && imgExtension != ".jpg")
                //    return BadRequest("Only .png and .jpg pages are allowed");

                //OR

                List<string> allowedExtensions = new List<string>() { ".png", ".jpg" };
                if (allowedExtensions.Contains(imgExtension) == false)
                    return BadRequest("Only .png and .jpg pages are allowed");

                if (product.ImageFile.Length > 1048576)
                    return BadRequest("Allow image withm aximum size 1 MB ");

                Guid imgGuid = Guid.NewGuid();
                string imgName = imgGuid + imgExtension;
                string imageUrl = "\\images\\" + imgName;
                product.ImageUrl = imageUrl;
                string imgPath = _webHostEnvironment.WebRootPath + imageUrl;

                FileStream imgStream = new FileStream(imgPath, FileMode.Create);
                product.ImageFile.CopyTo(imgStream);
                imgStream.Dispose();
            }

            if (product.Name.Trim() == product.Description.Trim())
            {
                ModelState.AddModelError("NameAndDescription","Name and Description mustn't match");
                return BadRequest(ModelState);
            }

            // To prevent duplicated prodcuts names
            if (_context.Products.Any(p => p.Name == product.Name))
            {
                ModelState.AddModelError("DuplicatedNames", "This product name is already exsit");
                return BadRequest(ModelState);
            }

            product.CreatedAt = DateTime.Now;
            _context.Products.Add(product);
            _context.SaveChanges();

            return CreatedAtRoute("GetById", new {id = product.Id}, product);
        }

        /// <summary>
        /// Edit/Update the current product using id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpPut("{id}")]
        public ActionResult PutProduct(int id, [FromForm]Product product)
        {
            if (product == null || id != product.Id)
                return NotFound();

            if (product.ImageFile != null)
            {
                string imgExtension = Path.GetExtension(product.ImageFile.FileName);

                List<string> allowedExtensions = new List<string>() { ".png", ".jpg" };
                if (allowedExtensions.Contains(imgExtension) == false)
                    return BadRequest("Only .png and .jpg pages are allowed");

                if (product.ImageFile.Length > 1048576)
                    return BadRequest("Allow image withm aximum size 1 MB ");

                if (product.ImageUrl != "\\images\\No_Image.png")
                {
                    string oldImgPath = _webHostEnvironment.WebRootPath + product.ImageUrl;
                    if (System.IO.File.Exists(oldImgPath))
                        System.IO.File.Delete(oldImgPath);
                }

                Guid imgGuid = Guid.NewGuid();
                string imgName = imgGuid + imgExtension;
                string imageUrl = "\\images\\" + imgName;
                product.ImageUrl = imageUrl;
                string imgPath = _webHostEnvironment.WebRootPath + imageUrl;

                FileStream imgStream = new FileStream(imgPath, FileMode.Create);
                product.ImageFile.CopyTo(imgStream); // Make a copy in images file
                imgStream.Dispose();
            }

            if (product.Name.Trim() == product.Description.Trim())
            {
                ModelState.AddModelError("NameAndDescription", "Name and Description mustn't be the same");
                return BadRequest(ModelState);
            }

            // To prevent duplicated prodcuts names
            if (_context.Products.Any(p => p.Name == product.Name) && product.Id != product.Id)
            {
                ModelState.AddModelError("DuplicatedNames", "This product name is already exsit");
                return BadRequest(ModelState);
            }

            product.LastUpdatedAt = DateTime.Now;
            _context.Products.Update(product);
            _context.SaveChanges();
            return NoContent();
        }

        /// <summary>
        /// Delete product permannetly form database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpDelete("{id}")]
        public ActionResult DeleteProduct(int? id)
        {
            if (id == null || id == 0)
                return BadRequest();
            var product = _context.Products.FirstOrDefault(x => x.Id == id);

            if(product == null)
                return NotFound();

            if(product.ImageUrl != "\\images\\No_Image.png")
            {
                string imgPath = _webHostEnvironment.WebRootPath + product.ImageUrl;
                if(System.IO.File.Exists(imgPath)) // Check if the exsit or not
                    System.IO.File.Delete(imgPath); // Delete the whole path
            }

            _context.Products.Remove(product);
            _context.SaveChanges();
            return NoContent();

        }
    }
}
