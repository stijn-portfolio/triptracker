using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using TripTracker.App.Models;
using TripTracker.App.ViewModels;
using Color = Mapsui.Styles.Color;

namespace TripTracker.App.Views;

// Code-behind bevat Mapsui setup - dit is een UITZONDERING op de MVVM regel.
// Reden: Mapsui MapControl vereist programmatische initialisatie.
// Alle business logic blijft in MapViewModel.
public partial class MapPage : ContentPage
{
    private readonly IMapViewModel _viewModel;
    private MemoryLayer? _pinsLayer;

    // Kleurenpalet voor verschillende trips
    private static readonly Color[] TripColors = new[]
    {
        new Color(81, 43, 212),    // Paars
        new Color(233, 30, 99),    // Roze
        new Color(76, 175, 80),    // Groen
        new Color(255, 152, 0),    // Oranje
        new Color(33, 150, 243),   // Blauw
        new Color(156, 39, 176),   // Violet
        new Color(0, 188, 212),    // Cyan
        new Color(244, 67, 54),    // Rood
    };

    // TripId → kleur index mapping
    private readonly Dictionary<int, int> _tripColorIndex = new();

    public MapPage(IMapViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Mapsui basis setup
        SetupMap();

        // Luister naar StopsUpdated event (fired 1x na alle stops geladen)
        _viewModel.StopsUpdated += () =>
        {
            if (_viewModel.Stops.Any())
            {
                UpdatePins(_viewModel.Stops);
            }
        };
    }

    private void SetupMap()
    {
        // Verwijder debug widgets
        mapControl.Map.Widgets.Clear();

        // OpenStreetMap tegel laag
        mapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Pins layer met ThemeStyle - kleur gebaseerd op TripId in feature
        _pinsLayer = new MemoryLayer
        {
            Name = "Pins",
            Style = new Mapsui.Styles.Thematics.ThemeStyle(CreatePinStyleFromFeature)
        };
        mapControl.Map?.Layers.Add(_pinsLayer);
    }

    private IStyle CreatePinStyleFromFeature(IFeature feature)
    {
        // Haal kleur index op via TripId
        var colorIndex = 0;
        if (feature["TripId"] is int tripId && _tripColorIndex.TryGetValue(tripId, out var idx))
        {
            colorIndex = idx;
        }

        var color = TripColors[colorIndex % TripColors.Length];

        return new SymbolStyle
        {
            SymbolScale = 1.2,
            Fill = new Mapsui.Styles.Brush(color),
            Outline = new Pen(Color.White, 3),
            SymbolType = SymbolType.Ellipse
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Update pins wanneer page verschijnt
        if (_viewModel.Stops.Any())
        {
            UpdatePins(_viewModel.Stops);
        }
    }

    private void UpdatePins(IEnumerable<TripStop> stops)
    {
        if (_pinsLayer == null || mapControl.Map == null) return;

        // Reset kleur mapping
        _tripColorIndex.Clear();
        var colorIndex = 0;

        var features = new List<IFeature>();

        foreach (var stop in stops)
        {
            if (stop.Latitude != 0 && stop.Longitude != 0)
            {
                // Wijs kleur index toe per trip
                if (!_tripColorIndex.ContainsKey(stop.TripId))
                {
                    _tripColorIndex[stop.TripId] = colorIndex;
                    colorIndex++;
                }

                // Converteer lat/lon naar Web Mercator
                var point = SphericalMercator.FromLonLat(stop.Longitude, stop.Latitude);

                // Feature met TripId voor ThemeStyle
                var feature = new GeometryFeature(new NetTopologySuite.Geometries.Point(point.x, point.y));
                feature["TripId"] = stop.TripId;
                features.Add(feature);
            }
        }

        _pinsLayer.Features = features;

        // Zoom naar pins
        if (features.Count > 0 && _pinsLayer.Extent != null)
        {
            var extent = _pinsLayer.Extent;
            var minSize = 5000;
            var growX = Math.Max(extent.Width * 0.3, minSize);
            var growY = Math.Max(extent.Height * 0.3, minSize);
            var grownExtent = extent.Grow(growX, growY);

            mapControl.Map.Navigator.ZoomToBox(grownExtent);
        }

        // Refresh de map
        mapControl.Refresh();
    }
}
