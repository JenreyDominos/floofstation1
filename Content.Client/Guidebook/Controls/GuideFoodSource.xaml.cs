using System.Linq;
using Content.Client.Chemistry.EntitySystems;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.Controls;

[UsedImplicitly, GenerateTypedNameReferences]
public sealed partial class GuideFoodSource : BoxContainer, ISearchableControl
{
    private readonly IPrototypeManager _protoMan;
    private readonly SpriteSystem _sprites = default!;

    public GuideFoodSource(IPrototypeManager protoMan)
    {
        RobustXamlLoader.Load(this);
        _protoMan = protoMan;
        _sprites = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
    }

    public GuideFoodSource(EntityPrototype result, FoodSourceData entry, IPrototypeManager protoMan) : this(protoMan)
    {
        switch (entry)
        {
            case FoodButcheringData butchering:
                GenerateControl(butchering);
                break;
            case FoodSlicingData slicing:
                GenerateControl(slicing);
                break;
            case FoodRecipeData recipe:
                GenerateControl(recipe);
                break;
            case FoodReactionData reaction:
                GenerateControl(reaction);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entry), entry, null);
        }

        GenerateOutputs(result, entry);
    }

    private void GenerateControl(FoodButcheringData entry)
    {
        if (!_protoMan.TryIndex(entry.Butchered, out var ent))
        {
            SourceLabel.SetMessage(Loc.GetString("guidebook-food-unknown-proto", ("id", entry.Butchered)));
            return;
        }

        SetSource(ent);
        ProcessingLabel.Text = Loc.GetString("guidebook-food-processing-butchering");

        ProcessingTexture.Texture = entry.Type switch
        {
            ButcheringType.Knife => GetRsiTexture("/Textures/Objects/Weapons/Melee/kitchen_knife.rsi", "icon"),
            _ => GetRsiTexture("/Textures/Structures/meat_spike.rsi", "spike")
        };
    }

    private void GenerateControl(FoodSlicingData entry)
    {
        if (!_protoMan.TryIndex(entry.Sliced, out var ent))
        {
            SourceLabel.SetMessage(Loc.GetString("guidebook-food-unknown-proto", ("id", entry.Sliced)));
            return;
        }

        SetSource(ent);
        ProcessingLabel.Text = Loc.GetString("guidebook-food-processing-slicing");
        ProcessingTexture.Texture = GetRsiTexture("/Textures/Objects/Misc/utensils.rsi", "plastic_knife");
    }

    private void GenerateControl(FoodRecipeData entry)
    {
        if (!_protoMan.TryIndex(entry.Recipe, out var recipe))
        {
            SourceLabel.SetMessage(Loc.GetString("guidebook-food-unknown-proto", ("id", entry.Result)));
            return;
        }

        var combinedSolids = recipe.IngredientsSolids
            .Select(it => _protoMan.TryIndex<EntityPrototype>(it.Key, out var proto) ? FormatIngredient(proto, it.Value) : "")
            .Where(it => it.Length > 0);
        var combinedLiquids = recipe.IngredientsReagents
            .Select(it => _protoMan.TryIndex<ReagentPrototype>(it.Key, out var proto) ? FormatIngredient(proto, it.Value) : "")
            .Where(it => it.Length > 0);

        var combinedIngredients = string.Join("\n", combinedLiquids.Union(combinedSolids));
        SourceLabel.SetMessage(Loc.GetString("guidebook-food-processing-recipe", ("ingredients", combinedIngredients)));

        ProcessingTexture.Texture = GetRsiTexture("/Textures/Structures/Machines/microwave.rsi", "mw");
        ProcessingLabel.Text = Loc.GetString("guidebook-food-processing-cooking", ("time", recipe.CookTime));
    }

    private void GenerateControl(FoodReactionData entry)
    {
        if (!_protoMan.TryIndex(entry.Reaction, out var reaction))
        {
            SourceLabel.SetMessage(Loc.GetString("guidebook-food-unknown-proto", ("id", entry.Reaction)));
            return;
        }

        var combinedReagents = reaction.Reactants
            .Select(it => _protoMan.TryIndex<ReagentPrototype>(it.Key, out var proto) ? FormatIngredient(proto, it.Value.Amount) : "")
            .Where(it => it.Length > 0);

        SourceLabel.SetMessage(Loc.GetString("guidebook-food-processing-recipe", ("ingredients", string.Join("\n", combinedReagents))));
        ProcessingTexture.TexturePath = "/Textures/Interface/Misc/beakerlarge.png";
        ProcessingLabel.Text = Loc.GetString("guidebook-food-processing-reaction");
    }

    private Texture GetRsiTexture(string path, string state)
    {
        return _sprites.Frame0(new SpriteSpecifier.Rsi(new ResPath(path), state));
    }

    private void GenerateOutputs(EntityPrototype result, FoodSourceData entry)
    {
        OutputsLabel.Text = Loc.GetString("guidebook-food-output", ("name", result.Name), ("number", entry.OutputCount));
        OutputsTexture.Texture = _sprites.Frame0(result);
    }

    private void SetSource(EntityPrototype ent)
    {
        SourceLabel.SetMessage(ent.Name);
        OutputsTexture.Texture = _sprites.Frame0(ent);
    }

    private string FormatIngredient(EntityPrototype proto, FixedPoint2 amount)
    {
        return Loc.GetString("guidebook-food-ingredient-solid", ("name", proto.Name), ("amount", amount));
    }

    private string FormatIngredient(ReagentPrototype proto, FixedPoint2 amount)
    {
        return Loc.GetString("guidebook-food-ingredient-liquid", ("name", proto.LocalizedName), ("amount", amount));
    }

    public bool CheckMatchesSearch(string query)
    {
        return this.ChildrenContainText(query);
    }

    public void SetHiddenState(bool state, string query)
    {
        Visible = CheckMatchesSearch(query) ? state : !state;
    }
}