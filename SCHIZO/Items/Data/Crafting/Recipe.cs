using System.Collections.Generic;
using System.Linq;
using Nautilus.Crafting;

namespace SCHIZO.Items.Data.Crafting;

partial class Recipe
{
    private RecipeData _converted;

    public RecipeData Convert()
    {
        if (_converted != null) return _converted;

        return _converted = new RecipeData
        {
            craftAmount = craftAmount,
            Ingredients = new List<NIngredient>(ingredients.Where(Ingredient.IsValid).Select(t => t.Convert())),
            LinkedItems = new List<TechType>(linkedItems.Where(Item.IsValid).Select(t => t.GetTechType()))
        };
    }
}
