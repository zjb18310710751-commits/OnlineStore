using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.ViewComponents;

public class HeroCarouselViewComponent(IHeroSlideService slides) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        View("~/Views/Home/_HeroCarousel.cshtml", await slides.GetAllAsync(activeOnly: true));
}
