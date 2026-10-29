using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using OnlineStore.Areas.Admin.Controllers;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Controllers;

public class HeroSlidesControllerTests
{
    [Fact]
    public async Task Create_RequiresUploadedImage_AndDoesNotTrustPostedImageUrl()
    {
        var service = new RecordingHeroSlideService();
        var images = new RecordingImageStorage();
        var controller = CreateController(service, images);
        var form = ValidForm();
        form.ImageUrl = "/images/existing.png";

        var result = await controller.Create(form);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(form.ImageFile)));
        Assert.Null(service.Created);
        Assert.Equal(0, images.SaveCount);
    }

    [Theory]
    [InlineData("https://example.com/products")]
    [InlineData("//example.com/products")]
    [InlineData("/\\example.com/products")]
    [InlineData("javascript:alert(1)")]
    [InlineData("Products")]
    [InlineData("/Products\n")]
    public async Task Create_RejectsUnsafeDestination_BeforeSavingImage(string destination)
    {
        var service = new RecordingHeroSlideService();
        var images = new RecordingImageStorage();
        var controller = CreateController(service, images);
        var form = ValidForm(withImage: true);
        form.LinkUrl = destination;

        var result = await controller.Create(form);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(form.LinkUrl)));
        Assert.Null(service.Created);
        Assert.Equal(0, images.SaveCount);
    }

    [Fact]
    public async Task Create_ImageValidationFailure_ReturnsFormWithoutWritingSlide()
    {
        var service = new RecordingHeroSlideService();
        var images = new RecordingImageStorage { RejectUpload = true };
        var controller = CreateController(service, images);

        var result = await controller.Create(ValidForm(withImage: true));

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(HeroSlideFormViewModel.ImageFile)));
        Assert.Null(service.Created);
    }

    [Fact]
    public async Task Create_PersistsUploadedImageAndContent()
    {
        var service = new RecordingHeroSlideService();
        var images = new RecordingImageStorage();
        var controller = CreateController(service, images);
        var form = ValidForm(withImage: true);
        form.HeadingEn = "A brighter day";
        form.DescriptionEn = "Find your favourites.";
        form.LinkUrl = "/Products?categoryId=2";
        form.DisplayOrder = 4;
        form.IsActive = false;

        var result = await controller.Create(form);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(service.Created);
        Assert.Equal(images.SavedUrl, service.Created.ImageUrl);
        Assert.Equal("A brighter day", service.Created.HeadingEn);
        Assert.Equal("Find your favourites.", service.Created.DescriptionEn);
        Assert.Equal("/Products?categoryId=2", service.Created.LinkUrl);
        Assert.Equal(4, service.Created.DisplayOrder);
        Assert.False(service.Created.IsActive);
    }

    [Fact]
    public async Task Edit_WithoutNewUpload_KeepsStoredImageAndUpdatesText()
    {
        var service = new RecordingHeroSlideService { Existing = ExistingSlide() };
        var images = new RecordingImageStorage();
        var controller = CreateController(service, images);
        var form = ValidForm();
        form.Id = 7;
        form.ImageUrl = "/uploads/forged.png";
        form.Heading = "Updated headline";

        var result = await controller.Edit(7, form);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(service.Updated);
        Assert.Equal("/images/campaign-earbuds.png", service.Updated.ImageUrl);
        Assert.Equal("Updated headline", service.Updated.Heading);
        Assert.Equal(0, images.SaveCount);
        Assert.Empty(images.Deleted);
    }

    [Fact]
    public async Task Edit_InvalidForm_RestoresStoredPreviewAndDoesNotModifySlide()
    {
        var service = new RecordingHeroSlideService { Existing = ExistingSlide() };
        var images = new RecordingImageStorage();
        var controller = CreateController(service, images);
        var form = ValidForm(withImage: true);
        form.Id = 7;
        form.ImageUrl = "/uploads/forged.png";
        controller.ModelState.AddModelError(nameof(form.Heading), "Heading is required.");

        var result = Assert.IsType<ViewResult>(await controller.Edit(7, form));

        var returned = Assert.IsType<HeroSlideFormViewModel>(result.Model);
        Assert.Equal("/images/campaign-earbuds.png", returned.ImageUrl);
        Assert.Null(service.Updated);
        Assert.Equal("Original headline", service.Existing.Heading);
        Assert.Equal(0, images.SaveCount);
    }

    [Fact]
    public async Task Edit_MismatchedRouteId_DoesNotUploadOrUpdate()
    {
        var service = new RecordingHeroSlideService { Existing = ExistingSlide() };
        var images = new RecordingImageStorage();
        var controller = CreateController(service, images);
        var form = ValidForm(withImage: true);
        form.Id = 7;

        Assert.IsType<NotFoundResult>(await controller.Edit(8, form));
        Assert.Null(service.Updated);
        Assert.Equal(0, images.SaveCount);
    }

    [Fact]
    public async Task Edit_MissingSlide_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingHeroSlideService(), new RecordingImageStorage());

        Assert.IsType<NotFoundResult>(await controller.Edit(999));
    }

    [Fact]
    public async Task Edit_InvalidReplacementImage_DoesNotReplaceCurrentContentOrDeleteImage()
    {
        var service = new RecordingHeroSlideService { Existing = ExistingSlide() };
        var images = new RecordingImageStorage { RejectUpload = true };
        var controller = CreateController(service, images);
        var form = ValidForm(withImage: true);
        form.Id = 7;

        var result = Assert.IsType<ViewResult>(await controller.Edit(7, form));

        Assert.Equal("/images/campaign-earbuds.png", Assert.IsType<HeroSlideFormViewModel>(result.Model).ImageUrl);
        Assert.Equal("Original headline", service.Existing.Heading);
        Assert.Null(service.Updated);
        Assert.Empty(images.Deleted);
    }

    [Fact]
    public async Task Delete_RemovesRequestedSlideWithoutDeletingPossiblySharedImage()
    {
        var service = new RecordingHeroSlideService { Existing = ExistingSlide() };
        var images = new RecordingImageStorage();
        var controller = CreateController(service, images);

        Assert.IsType<RedirectToActionResult>(await controller.Delete(7));
        Assert.Equal(7, service.DeletedId);
        Assert.Empty(images.Deleted);
        Assert.IsType<NotFoundResult>(await controller.Delete(999));
    }

    [Fact]
    public async Task Create_DatabaseFailure_CleansNewUpload()
    {
        var service = new RecordingHeroSlideService { FailWrites = true };
        var images = new RecordingImageStorage();
        var controller = CreateController(service, images);

        await Assert.ThrowsAsync<IOException>(() => controller.Create(ValidForm(withImage: true)));

        Assert.Equal(images.SavedUrl, Assert.Single(images.Deleted));
    }

    [Fact]
    public void ManagementActions_RequireAdminAndPostAntiforgery()
    {
        var type = typeof(HeroSlidesController);
        Assert.Contains(type.GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Roles == "Admin");
        Assert.Empty(type.GetCustomAttributes<AllowAnonymousAttribute>());
        var mutations = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<HttpPostAttribute>() is not null).ToArray();
        Assert.Equal(new[] { "Create", "Delete", "Edit" }, mutations.Select(method => method.Name).OrderBy(name => name));
        Assert.All(mutations, method => Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
        Assert.All(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            method => Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>()));
    }

    private static HeroSlide ExistingSlide() => new()
    {
        Id = 7, Title = "Original collection", Heading = "Original headline",
        ImageUrl = "/images/campaign-earbuds.png", LinkUrl = "/Products", IsActive = true
    };

    private static HeroSlideFormViewModel ValidForm(bool withImage = false) => new()
    {
        Title = "Seasonal collection", Heading = "Make the day yours", LinkUrl = "/Products", IsActive = true,
        ImageFile = withImage ? new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "ImageFile", "hero.png")
        {
            Headers = new HeaderDictionary(), ContentType = "image/png"
        } : null
    };

    private static HeroSlidesController CreateController(IHeroSlideService service, IImageStorageService images)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor());
        return new HeroSlidesController(service, images)
        {
            ControllerContext = new ControllerContext(actionContext),
            Url = new UrlHelper(actionContext),
            TempData = new TempDataDictionary(actionContext.HttpContext, new NullTempDataProvider())
        };
    }

    private sealed class RecordingHeroSlideService : IHeroSlideService
    {
        public HeroSlide? Existing { get; init; }
        public HeroSlide? Created { get; private set; }
        public HeroSlide? Updated { get; private set; }
        public int? DeletedId { get; private set; }
        public bool FailWrites { get; init; }
        public Task<IReadOnlyList<HeroSlide>> GetAllAsync(bool activeOnly = false) =>
            Task.FromResult<IReadOnlyList<HeroSlide>>(Existing is null ? Array.Empty<HeroSlide>() : new[] { Existing });
        public Task<PagedResult<HeroSlide>> GetPagedAsync(int page, int pageSize)
        {
            var items = Existing is null ? Array.Empty<HeroSlide>() : new[] { Existing };
            return Task.FromResult(new PagedResult<HeroSlide>(items, page, pageSize, items.Length));
        }
        public Task<int> CountActiveAsync() => Task.FromResult(Existing?.IsActive == true ? 1 : 0);
        public Task<HeroSlide?> GetByIdAsync(int id) => Task.FromResult(Existing?.Id == id ? Existing : null);
        public Task<HeroSlide> CreateAsync(HeroSlide slide)
        {
            if (FailWrites) throw new IOException("Simulated database write failure.");
            Created = slide;
            return Task.FromResult(slide);
        }
        public Task UpdateAsync(HeroSlide slide) { Updated = slide; return Task.CompletedTask; }
        public Task<bool> DeleteAsync(int id) { DeletedId = id; return Task.FromResult(Existing?.Id == id); }
    }

    private sealed class RecordingImageStorage : IImageStorageService
    {
        public string SavedUrl => "/uploads/new-hero.png";
        public bool RejectUpload { get; init; }
        public int SaveCount { get; private set; }
        public List<string?> Deleted { get; } = new();
        public Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (RejectUpload) throw new InvalidOperationException("Invalid image.");
            return Task.FromResult(SavedUrl);
        }
        public void Delete(string? relativeUrl) => Deleted.Add(relativeUrl);
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
