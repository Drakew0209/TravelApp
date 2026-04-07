using TravelApp.Models;

namespace TravelApp.Services;

public class MockDataService
{
    /// <summary>
    /// HCM Food Tour - Starting point and first waypoint
    /// </summary>
    public static List<PoiModel> GetForYouData()
    {
        return new List<PoiModel>
        {
            new PoiModel
            {
                Id = 1,
                Title = "Chợ Bến Thành",
                Subtitle = "Food Tour HCM - Starting Point",
                ImageUrl = "https://images.unsplash.com/photo-1555521760-cb7ebb6a9c62?w=800&h=600&fit=crop",
                Location = "Chợ Bến Thành, Quận 1, TPHCM",
                Distance = "0 km",
                Duration = "45 min",
                Provider = "TravelApp",
                Description = "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn với đa dạng hàng hóa và đặc biệt là các quán ăn địa phương.",
                Credit = "Photo from Unsplash"
            },
            new PoiModel
            {
                Id = 2,
                Title = "Phở Vĩnh Khánh",
                Subtitle = "Food Tour HCM - Pho Experience",
                ImageUrl = "https://images.unsplash.com/photo-1565030826693-9d4595707d90?w=800&h=600&fit=crop",
                Location = "Phố Vĩnh Khánh, Quận 4, TPHCM",
                Distance = "0.9 km",
                Duration = "30 min",
                Provider = "TravelApp",
                Description = "Quán phở nổi tiếng với nước dùng được ninh từ 12h, phục vụ phở bò ngon nhất Quận 4.",
                Credit = "Photo from Unsplash"
            }
        };
    }

    /// <summary>
    /// Hanoi Food Tour - Starting point and first waypoint
    /// </summary>
    public static List<PoiModel> GetEditorsChoiceData()
    {
        return new List<PoiModel>
        {
            new PoiModel
            {
                Id = 4,
                Title = "Chùa Một Cột",
                Subtitle = "Food Tour Hanoi - Starting Point",
                ImageUrl = "https://images.unsplash.com/photo-1511632765486-a01980e01a18?w=800&h=600&fit=crop",
                Location = "Chùa Một Cột, Quận Ba Đình, Hà Nội",
                Distance = "0 km",
                Duration = "45 min",
                Provider = "TravelApp",
                Description = "Điểm khởi đầu của tour ẩm thực Hà Nội. Chùa Một Cột là một di tích lịch sử quan trọng, nằm gần khu phố cổ Hà Nội.",
                Credit = "Photo from Unsplash"
            },
            new PoiModel
            {
                Id = 5,
                Title = "Phố Hàng Xanh",
                Subtitle = "Food Tour Hanoi - Local Cuisine",
                ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561d1b?w=800&h=600&fit=crop",
                Location = "Phố Hàng Xanh, Quận Hoàn Kiếm, Hà Nội",
                Distance = "0.3 km",
                Duration = "45 min",
                Provider = "TravelApp",
                Description = "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội với các quán ăn truyền thống.",
                Credit = "Photo from Unsplash"
            }
        };
    }

    public static PoiModel? GetById(int id)
    {
        return GetForYouData().Concat(GetEditorsChoiceData()).FirstOrDefault(x => x.Id == id);
    }
}
