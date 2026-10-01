using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface IBusinessService
    {
        Task<Business> CreateBusiness(int ownerId, CreateBusinessRequest request);
        Task<Business> UpdateBusiness(int businessId, int userId, UpdateBusinessRequest request);
        Task<Business> GetBusinessById(int businessId);
        Task<Business> GetBusinessByEmail(string email);
        Task<Business> GetBusinessByPhone(string phone);
        Task<List<Business>> GetAllBusinesses(string category = null, string status = null);
        Task<List<Business>> GetUserBusinesses(int userId);
        Task<bool> DeleteBusiness(int businessId, int userId);
        Task<BusinessReview> AddReview(int businessId, int userId, CreateReviewRequest request);
        Task<bool> FollowBusiness(int businessId, int userId);
        Task<bool> UnfollowBusiness(int businessId, int userId);
        Task<BusinessPost> CreatePost(int businessId, int userId, CreatePostRequest request);
        Task<List<Business>> GetAllBusinesses();
        Task<BusinessProduct> AddProduct(int businessId, int userId, CreateBusinessProductRequest request);
        Task<BusinessProduct> UpdateProduct(int businessId, int productId, int userId, UpdateBusinessProductRequest request);
        Task<bool> DeleteProduct(int businessId, int productId, int userId);
        Task<List<Business>> SearchBusinesses(
            string? q = null,
            string? category = null,
            string? subcategory = null,
            string? area = null,
            string? road = null,
            string? street = null,
            double? latitude = null,
            double? longitude = null,
            double? radiusKm = null);
    }
}

