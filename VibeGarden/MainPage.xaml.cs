using Microsoft.Maui.Controls.Shapes;
using System.Security.Cryptography;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Plugin.Maui.Audio;
using VibeGardeb.Models;
#if ANDROID
using VibeGardeb.Platforms.Android;
#endif

using VibeGardeb.Services;
using Path = System.IO.Path;
using View = Microsoft.Maui.Controls.View;
using TextAlignment = Microsoft.Maui.TextAlignment;

namespace VibeGardeb
{
    public partial class MainPage : ContentPage
    {
        private GameData gameData;
        private GameDataService gameDataService;

        private readonly Random grassRandom = new Random();
        private CancellationTokenSource? grassAnimationCts;

        private readonly Dictionary<FlowerPack, Label> cantAffordLabels = new();
        private readonly List<Image> grassBlades = new List<Image>();

        private bool packOpeningInProgress = false;

        private IAudioPlayer shopMusicPlayer;

        private Grid? packAnimationOverlay;

        private Grid shopGrid;


        FlowerPackAnimationService animationService;

        private FlowerPackService flowerPackService;

        private FlowerMoneyService flowerMoneyService;

        private List<InventoryFlowerObject> seedInventory;

        private Label growthTimeLabel;


        private Flower activeFlower;

        private IDispatcherTimer growthTimer;

        private IDispatcherTimer flowerPotCheckTimer;

        private FlowerType selectedFlower;

        private List<Flower> flowerPotContent;

        private int coinAmount;

        private bool shovelMode = false;
        private bool waterMode = false;
        private bool shopMode = false;
        private bool coinAnimationPlaying = false;

        public MainPage()
        {
            InitializeComponent();

            CreateGrass();

            //animation List service
            animationService = new FlowerPackAnimationService();

            // player inventory
            seedInventory = new List<InventoryFlowerObject>();

            // DateTime moneyservice
            flowerMoneyService = new FlowerMoneyService();

            // inhoud van de bloempot
            flowerPotContent = new List<Flower>();

            NavigationPage.SetHasNavigationBar(this, false);

            // laad de service in voor saving
            gameDataService = new GameDataService();

            // laad save data terug in
            LoadGameData();

            //laad de packservice
            flowerPackService = new FlowerPackService();


 


        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            


            shopMusicPlayer.Stop();
        }

        private void StopShopMusic()
        {
            try
            {
                if (shopMusicPlayer != null)
                {
                    shopMusicPlayer.Stop();
                    shopMusicPlayer.Dispose();
                    shopMusicPlayer = null;
                }
            }
            catch
            {
                shopMusicPlayer = null;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (Window != null)
            {
                Window.Deactivated -= Window_Deactivated;
                Window.Stopped -= Window_Stopped;
                Window.Destroying -= Window_Destroying;

                Window.Deactivated += Window_Deactivated;
                Window.Stopped += Window_Stopped;
                Window.Destroying += Window_Destroying;
            }

            StartGrassAnimation();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            StopShopMusic();
        }

        private void Window_Stopped(object sender, EventArgs e)
        {
            StopShopMusic();
        }

        private void Window_Destroying(object sender, EventArgs e)
        {
            StopShopMusic();
        }


        // =========================================================
        // BUTTON ACTIONS
        // =========================================================

        private void PlantButton_Clicked(object sender, EventArgs e)
        {

            AnimateButton((ImageButton)sender);
            if (shopMode)
            {
                shopMode = false;

                ShopItemsLayout.Children.Clear();

                ShopLayout.IsVisible = false;
                ShopLayout.IsEnabled = false;

                StopShopMusic();
                
            }

            if (selectedFlower != null)
            {
                selectedFlower = null;
                shovelMode = false;
                waterMode = false;

                LoadFlowerpotContent();
                return;
            }

                OpenInventory();
            
           
            
                shovelMode = false;
                waterMode = false;
                selectedFlower = null;

                LoadFlowerpotContent();
            
        }


        private void WaterButton_Clicked(object sender, EventArgs e)
        {

            AnimateButton((ImageButton)sender);
            bool containsFlowers = false;

            foreach (Flower flower in flowerPotContent)
            {
                if (flower.WateredAt == null)
                {
                    containsFlowers = true;
                    break;
                }
            }

          
            if (!containsFlowers)
            {
                waterMode = false;
                LoadFlowerpotContent();
                return;
            }

            if (waterMode)
            {
                waterMode = false;
                shovelMode = false;
                selectedFlower = null;

                LoadFlowerpotContent();
                return;
            }

            WaterAction();
        }


        private void ShovelButton_Clicked(object sender, EventArgs e)
        {

            AnimateButton((ImageButton)sender);
            if (shovelMode)
            {
                shovelMode = false;
                waterMode = false;
                selectedFlower = null;

                LoadFlowerpotContent();
                return;
            }

            ShovelAction();
        }


        // =========================================================
        // LOAD / SAVE GAME DATA
        // =========================================================

        private async void LoadGameData()
        {
            gameData = await gameDataService.LoadGameDataAsync();

            flowerPotContent =
                gameData.FlowerPotContent ?? new List<Flower>();

            seedInventory =
                gameData.SeedInventory ??
                new List<InventoryFlowerObject>();

            
            coinAmount = gameData.Coins;

            if (gameData.IsFirstLaunch)
            {

                coinAmount = 400;

                gameData.IsFirstLaunch = false;

                await SaveGameDataAsync();
            }

            LoadFlowerpotContent();

            StartFlowerPotCheckTimer();

            
            coinLabel.Text = FormatCoins(coinAmount);
        }

        private string FormatGrowthTime(TimeSpan remaining)
        {
            if (remaining.TotalHours >= 1)
            {
                return $"{remaining.TotalHours:0.00}H";
            }

            return $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
        }


        private async Task SaveGameDataAsync()
        {
            gameData.FlowerPotContent = flowerPotContent;
            gameData.SeedInventory = seedInventory;
            gameData.Coins = coinAmount;

            await gameDataService.SaveGameDataAsync(gameData);
        }


        // =========================================================
        // FLOWERPOT
        // =========================================================

        private void LoadFlowerpotContent()
        {
            FlowerGrid.Children.Clear();

            foreach (Flower flower in flowerPotContent)
            {
                if (flower.WateredAt != null &&
                    flowerMoneyService.IsReadyToCollect(flower))
                {
                    // Alleen status bepalen; nog NIET automatisch collecten.
                }
            }

            foreach (Flower flower in flowerPotContent)
            {
                ImageButton button = new ImageButton
                {
                    BackgroundColor = Colors.Transparent,
                    Padding = 0,
                    Margin = 0,
                    Scale = 1.6,
                    HorizontalOptions = LayoutOptions.Center,
                    Source = flower.Type.Sprite,
                    ZIndex = flower.Position.Row == 1 ? 2 : 1,
                    TranslationX = -4,
                };

                if (flower.Position.Row == 0)
                {
                    button.TranslationY = 5;
                }

                if (flower.Position.Row == 1)
                {
                    button.TranslationY = 9;
                }


                button.Clicked += async (sender, e) =>
                {
                    // WATER MODE
                    if (waterMode)
                    {
                        if (flower.WateredAt != null)
                            return;

                        flowerMoneyService.WaterFlower(flower);

                        ImageButton? waterSelect = FlowerGrid.Children
                        .OfType<ImageButton>()
                        .FirstOrDefault(x =>
                            x.AutomationId ==
                            $"waterselect_{flower.Position.Row}_{flower.Position.Column}");


                        if (waterSelect != null)
                        {
                            FlowerGrid.Children.Remove(waterSelect);
                        }

                        await PlayWaterDropAnimation(flower);

                        activeFlower = flower;
                        selectedFlower = null;

                        bool hasFlowersToWater = flowerPotContent.Any(
                            f => f.WateredAt == null
                        );

                        if (!hasFlowersToWater)
                        {
                            waterMode = false;
                        }

                        ShowFlowerInfo(flower);

                        await SaveGameDataAsync();

                        await Task.Delay(100);
                        await MainThread.InvokeOnMainThreadAsync(() => { });
                        await Task.Delay(50);

                        await SaveFlowerPotSnapshotAsync();

                        #if ANDROID
                        UpdateFlowerPotWidget();
                        #endif

                        return;
                    }

                    // SHOVEL MODE
                    if (shovelMode)
                    {
                        RemoveFlower(flower);
                        return;
                    }

                    // NORMALE MODE
                    IAudioPlayer player = AudioManager.Current.CreatePlayer(
                        await FileSystem.OpenAppPackageFileAsync("leaves.mp3")
                    );
                    player.Volume = 0.3;
                    player.Play();
                    await ShakePlant(button);


                    ShowFlowerInfo(flower);
                };

                FlowerGrid.Add(
                    button,
                    flower.Position.Column,
                    flower.Position.Row
                );


                // =====================================================
                // COIN BUTTON
                // =====================================================

                if (flower.WateredAt != null &&
                    flowerMoneyService.IsReadyToCollect(flower))
                {
                    var coinImage = new Image
                    {
                        Source = "coingif.gif",
                       
                        IsAnimationPlaying = true,
                        WidthRequest = 60,
                        HeightRequest = 60,
                        VerticalOptions = LayoutOptions.Start,
                        Margin = new Thickness(0, -20, 5, 0),
                        ZIndex = 3,
                    };

                    var tap = new TapGestureRecognizer();
                    tap.Tapped += async (s, e) =>
                    {
                        if (coinImage.Opacity < 1)
                            return;


                        await ClaimCoinAnimation(coinImage, flower);
                    };

                    coinImage.GestureRecognizers.Add(tap);

                    FlowerGrid.Add(
                        coinImage,
                        flower.Position.Column,
                        flower.Position.Row
                    );
                }
            }

            // =====================================================
            // WATER SELECT PNG
            // =====================================================

            if (waterMode)
            {
                foreach (Flower flower in flowerPotContent)
                {
                    if(flower.WateredAt == null)
                    {
                        ImageButton waterImage = new ImageButton
                        {
                            Source = "waterselect.png",
                            Aspect = Aspect.AspectFill,
                            Scale = 1.1,
                            BackgroundColor = Colors.Transparent,
                            Padding = 0,
                            Margin = 0,
                            TranslationX = -4,
                            InputTransparent = true,
                            ZIndex = 0,
                            AutomationId = $"waterselect_{flower.Position.Row}_{flower.Position.Column}"
                        };

                        if (flower.Position.Row == 0)
                        {
                            waterImage.TranslationY = 22;
                        }
                        else
                        {
                            waterImage.TranslationY = 25;
                        }

                        FlowerGrid.Add(
                            waterImage,
                            flower.Position.Column,
                            flower.Position.Row
                        );


                    }


                }
            }

            // =====================================================
            // SHOVEL SELECT PNG
            // =====================================================

            if (shovelMode)
            {
                foreach (Flower flower in flowerPotContent)
                {
                    ImageButton shovelImage = new ImageButton
                    {
                        Source = "shovelselect.png",
                        Aspect = Aspect.AspectFill,
                        Scale = 1.1,
                        BackgroundColor = Colors.Transparent,
                        Padding = 0,
                        Margin = 0,
                        TranslationX = -4,
                        InputTransparent = true,
                        ZIndex = 0
                    };

                    if (flower.Position.Row == 0)
                    {
                        shovelImage.TranslationY = 22;
                    }
                    else
                    {
                        shovelImage.TranslationY = 25;
                    }

                    FlowerGrid.Add(
                        shovelImage,
                        flower.Position.Column,
                        flower.Position.Row
                    );
                }
            }
        }


        // =========================================================
        // INVENTORY
        // =========================================================

        private void RefreshInventory()
        {
            InventoryLayout.Children.Clear();

            var rarityOrder = new Dictionary<FlowerRarity, int> { { FlowerRarity.Common, 0 }, { FlowerRarity.Uncommon, 1 }, { FlowerRarity.Rare, 2 }, { FlowerRarity.Epic, 3 }, { FlowerRarity.Legendary, 4 }, { FlowerRarity.Mythic, 5 } };

            var sortedSeeds = seedInventory.OrderBy(seed => rarityOrder.TryGetValue(seed.Flowertype.Rarity, out int order) ? order : int.MaxValue).ToList();

            foreach (InventoryFlowerObject seed in sortedSeeds)
            {
                Image seedImage = new Image
                {
                    Source = ImageSource.FromFile(
                        seed.Flowertype.SeedSprite
                    ),

                    WidthRequest = 65,
                    HeightRequest = 50,
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };


                Color rarityColor =
                RarityHelper.GetColor(seed.Flowertype.Rarity);


                Label nameImage = new Label
                {
                    TranslationY = 10,
                    Text = $"{seed.Flowertype.Name} x{seed.Quantity}",
                    WidthRequest = 100,
                    HeightRequest = 45,
                    FontSize = 15,
                    TextColor = rarityColor,
                    FontAttributes = FontAttributes.Bold,
                    FontFamily = "Minecraft",
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center,

                    Shadow = new Shadow
                    {
                        Brush = Colors.White,
                        Radius = 5,
                        Opacity = 0.8f,
                        Offset = new Point(0, 0)
                    }
                };


                Grid itemGrid = new Grid
                {
                    RowDefinitions =
                    {
                        new RowDefinition { Height = 70 },
                        new RowDefinition { Height = 15 }
                    }
                };


                itemGrid.Add(seedImage, 0, 0);
                itemGrid.Add(nameImage, 0, 0);

                Grid.SetColumn(seedImage, 0);
                Grid.SetColumn(nameImage, 0);


                BoxView divider = new BoxView
                {
                    Color = Color.FromArgb("#ffa224"),
                    HeightRequest = 3,
                    WidthRequest = 220,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };


                itemGrid.Add(divider, 0, 1);


                Border button = new Border
                {
                    Content = itemGrid,
                    HeightRequest = 80,
                    HorizontalOptions = LayoutOptions.Fill,
                    BackgroundColor = Colors.Transparent,
                    StrokeThickness = 0,
                    Margin = new Thickness(10, 3)
                };


                var tap = new TapGestureRecognizer();

                tap.Tapped += (s, e) =>
                {
                    selectedFlower = seed.Flowertype;

                    shovelMode = false;
                    waterMode = false;

                    AddFlowerButtons();
                };


                button.GestureRecognizers.Add(tap);

                InventoryLayout.Add(button);
            }
        }


        // =========================================================
        // GROWTH TIMER
        // =========================================================

        private async void UpdateGrowthTimer()
        {
            if (activeFlower == null)
                return;

            if (activeFlower.WateredAt == null)
            {
                StopGrowthTimer();
                return;
            }

            if (flowerMoneyService.IsReadyToCollect(activeFlower))
            {
                StopGrowthTimer();

                
                LoadFlowerpotContent();

                return;
            }

            TimeSpan remaining =
                flowerMoneyService.GetRemainingTime(activeFlower);

            if (growthTimeLabel != null)
            {
                growthTimeLabel.Text = FormatGrowthTime(remaining);
            }
        }

        private async Task CollectFlowerMoney(Flower flower)
        {
            if (!flowerMoneyService.IsReadyToCollect(flower))
                return;

            int money = flowerMoneyService.CollectFlower(flower);

            coinAmount += money;

            coinLabel.Text = coinAmount.ToString();

            flower.WateredAt = null;

            if (activeFlower == flower)
            {
                StopGrowthTimer();
                activeFlower = null;
            }

            flowerInfo.Children.Clear();

            await SaveGameDataAsync();

            LoadFlowerpotContent();
        }


        // =========================================================
        // WATER / SHOVEL MODE
        // =========================================================

        private void WaterAction()
        {
            shovelMode = false;

            waterMode = !waterMode;

            selectedFlower = null;

            LoadFlowerpotContent();
        }


        private void ShovelAction()
        {
            shovelMode = !shovelMode;

            waterMode = false;

            selectedFlower = null;

            LoadFlowerpotContent();
        }


        // =========================================================
        // FLOWER INFO
        // =========================================================

        private void ShowFlowerInfo(Flower flower)
        {
            StopGrowthTimer();

            flowerInfo.Children.Clear();

            activeFlower = flower;


            Color rarityColor =
              RarityHelper.GetColor(flower.Type.Rarity);

            Label label = new Label
            {
                Text = flower.Type.Name,
                FontFamily = "Minecraft",
                TextColor = rarityColor,
                FontSize = 25,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TranslationX = 10,

                Shadow = new Shadow
                {
                    Brush = Colors.White,
                    Radius = 5,
                    Opacity = 0.8f,
                    Offset = new Point(0, 0)
                }
            };


            flowerInfo.Children.Add(label);

            Grid.SetRow(label, 0);


            // -----------------------------------------------------
            // FLOWER IS WATERED
            // -----------------------------------------------------

            if (flower.WateredAt != null)
            {
                Grid clockGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition
                        {
                            Width = 60
                        },

                        new ColumnDefinition
                        {
                            Width = GridLength.Auto
                        },

                        new ColumnDefinition
                        {
                            Width = GridLength.Auto
                        },

                        new ColumnDefinition
                        {
                            Width = GridLength.Auto
                        }
                    },

                    HorizontalOptions = LayoutOptions.Center
                };


                Image clockImage = new Image
                {
                    Source = "clock.png",
                    WidthRequest = 75,
                    HeightRequest = 75,
                    Margin = new Thickness(0, 0, 10, 0),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };


                TimeSpan remaining =
                    flowerMoneyService.GetRemainingTime(
                        flower
                    );


                growthTimeLabel = new Label
                {
                    Text = FormatGrowthTime(remaining),

                    FontFamily = "Minecraft",
                    TextColor = Colors.Yellow,
                    FontSize = 20,

                    HorizontalOptions =
                        LayoutOptions.Start,

                    VerticalOptions =
                        LayoutOptions.Center
                };
                Label dividerLabel = new Label
                {
                    Text = "|",
                    FontFamily = "Minecraft",
                    TextColor = Colors.White,
                    FontSize = 20,

                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,

                    Margin = new Thickness(8, 0)
                };

                Image moneyGainImage = new Image
                {
                    Source = "stillcoin.png",
                    WidthRequest = 25,
                    HeightRequest = 25,

                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    
                    Margin = new Thickness(5, 0, 0, 0)
                };

                Label moneyGainLabel = new Label
                {
                    Text = flower.Type.MoneyGain.ToString(),
                    FontFamily = "Minecraft",
                    TextColor = Colors.White,
                    FontSize = 20,

                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };



                clockGrid.Add(
                    clockImage,
                    0,
                    0
                );

                clockGrid.Add(
                    growthTimeLabel,
                    1,
                    0
                );

                clockGrid.Add(
                    dividerLabel,
                    2,
                    0
                );

                HorizontalStackLayout moneyGainLayout = new HorizontalStackLayout
                {
                    Spacing = 2,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                moneyGainLayout.Add(moneyGainLabel);
                moneyGainLayout.Add(moneyGainImage);

                clockGrid.Add(
                    moneyGainLayout,
                    3,
                    0
                );


                flowerInfo.Children.Add(clockGrid);

                Grid.SetRow(clockGrid, 1);


                StartGrowthTimer();
            }
            else
            {
                StopGrowthTimer();
            }
        }


        // =========================================================
        // REMOVE FLOWER
        // =========================================================

        private async void RemoveFlower(Flower flower)
        {
            IAudioPlayer player = AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync("delete.mp3")
            );
            player.Volume = 0.2;
            player.Play();
            flowerPotContent.Remove(flower);

            AddSeedToInventory(flower.Type);

            activeFlower = null;

            StopGrowthTimer();

            flowerInfo.Children.Clear();

            await SaveGameDataAsync();

            
            waterMode = false;
            selectedFlower = null;

            LoadFlowerpotContent();

            if (Inventory.IsVisible)
            {
                RefreshInventory();
            }

            await Task.Delay(50);

            await SaveFlowerPotSnapshotAsync();

#if ANDROID
            UpdateFlowerPotWidget();
#endif
        }


        // =========================================================
        // PLANT FLOWER
        // =========================================================

        private async void PlantFlower(int row, int column)
        {
            if (selectedFlower == null)
                return;


            shovelMode = false;
            waterMode = false;


            InventoryFlowerObject inventoryItem = null;


            foreach (InventoryFlowerObject check in seedInventory)
            {
                if (check.Flowertype.Name ==
                    selectedFlower.Name)
                {
                    inventoryItem = check;
                    break;
                }
            }


            if (inventoryItem == null ||
                inventoryItem.Quantity <= 0)
            {
                selectedFlower = null;
                return;
            }


            FlowerPosition position =
                new FlowerPosition(row, column);


            Flower flower =
                new Flower(
                    selectedFlower,
                    position
                );

            IAudioPlayer player = AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync("plant.mp3")
            );
            player.Volume = 0.2;
            player.Play();
            flowerPotContent.Add(flower);

            inventoryItem.Quantity--;


            if (inventoryItem.Quantity <= 0)
            {
                seedInventory.Remove(inventoryItem);
            }


            await SaveGameDataAsync();


            selectedFlower = null;
            shovelMode = false;
            waterMode = false;


            LoadFlowerpotContent();

            RefreshInventory();


            await Task.Delay(50);

            await SaveFlowerPotSnapshotAsync();

#if ANDROID
            UpdateFlowerPotWidget();
#endif
        }


        // =========================================================
        // FLOWER POSITION
        // =========================================================

        private bool IsFlowerSpotOccupied(
            int row,
            int column)
        {
            return flowerPotContent.Any(flower =>
                flower.Position.Row == row &&
                flower.Position.Column == column);
        }


        // =========================================================
        // INVENTORY OPENEN
        // =========================================================

        private void OpenInventory()
        {
            if (Inventory.IsEnabled == true &&
                Inventory.IsVisible == true)
            {
                Inventory.IsVisible = false;

                Inventory.IsEnabled = false;

                InventoryLayout.Children.Clear();
            }
            else
            {
                Inventory.IsVisible = true;

                Inventory.IsEnabled = true;

                RefreshInventory();
            }
        }


        // =========================================================
        // SEEDS TOEVOEGEN
        // =========================================================

        private void AddSeedToInventory(
            FlowerType flowerType,
            int quantity = 1)
        {
            InventoryFlowerObject inventoryItem = null;


            foreach (InventoryFlowerObject item in seedInventory)
            {
                if (item.Flowertype.Name ==
                    flowerType.Name)
                {
                    inventoryItem = item;
                    break;
                }
            }


            if (inventoryItem != null)
            {
                inventoryItem.Quantity += quantity;
            }
            else
            {
                InventoryFlowerObject newItem =
                    new InventoryFlowerObject(
                        flowerType,
                        quantity
                    );

                seedInventory.Add(newItem);
            }
        }


        // =========================================================
        // PLANT BUTTONS
        // =========================================================

        private void AddFlowerButtons()
        {
            FlowerGrid.Children.Clear();

            LoadFlowerpotContent();


            for (int row = 0; row < 2; row++)
            {
                for (int column = 0; column < 5; column++)
                {
                    if (IsFlowerSpotOccupied(
                        row,
                        column))
                    {
                        continue;
                    }


                    int selectedRow = row;
                    int selectedColumn = column;


                    ImageButton button = new ImageButton
                    {
                        BackgroundColor =
                            Colors.Transparent,

                        Padding = 0,

                        Margin = 0,

                        Scale = 1.2,

                        Source = "plantselect.png",

                        ZIndex =
                            selectedRow == 1
                                ? 2
                                : 1
                    };


                    if (selectedRow == 0)
                    {
                        button.TranslationY = 23;
                    }
                    else
                    {
                        button.TranslationY = 30;
                    }


                    button.Clicked +=
                        (sender, e) =>
                        {
                            PlantFlower(
                                selectedRow,
                                selectedColumn
                            );
                        };


                    FlowerGrid.Add(
                        button,
                        selectedColumn,
                        selectedRow
                    );
                }
            }
        }


        // =========================================================
        // PLANT ANIMATIE
        // =========================================================

        private async Task ShakePlant(
            VisualElement plant)
        {
            double originalRotation =
                plant.Rotation;


            await plant.RotateTo(
                -3,
                70,
                Easing.SinInOut
            );


            await plant.RotateTo(
                3,
                100,
                Easing.SinInOut
            );


            await plant.RotateTo(
                originalRotation,
                70,
                Easing.SinInOut
            );
        }


        private async Task ClaimCoinAnimation(
    Image coinImage,
    Flower flower)
        {
            double originalTranslationY = coinImage.TranslationY;

            coinImage.Opacity = 1;
            coinImage.Scale = 1;

            IAudioPlayer player = AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync("coinsound.mp3")
            );
            player.Volume = 0.2;
            player.Play();

            await coinImage.ScaleTo(
                1.1,
                100,
                Easing.SinOut
            );

            Task moveUp = coinImage.TranslateTo(
                coinImage.TranslationX,
                originalTranslationY - 25,
                350,
                Easing.CubicOut
            );

            Task fadeOut = coinImage.FadeTo(
                0,
                350,
                Easing.CubicIn
            );

            await Task.WhenAll(moveUp, fadeOut);

            // Alleen deze coin verwijderen/collecten
            int money = flowerMoneyService.CollectFlower(flower);

            coinAmount += money;
            coinLabel.Text = FormatCoins(coinAmount);


            flower.WateredAt = null;

            if (activeFlower == flower)
            {
                StopGrowthTimer();
                activeFlower = null;
            }

            flowerInfo.Children.Clear();

            await SaveGameDataAsync();

            // Alleen deze coin uit de grid halen
            FlowerGrid.Children.Remove(coinImage);
        }

        private string FormatCoins(int coins)
        {
            if (coins < 1000)
                return coins.ToString();

            if (coins < 1_000_000)
            {
                double value = coins / 1000.0;

                
                return value % 1 == 0
                    ? $"{value:0}K"
                    : $"{value:0.0}K";
            }

            if (coins < 1_000_000_000)
            {
                double value = coins / 1_000_000.0;

                return value % 1 == 0
                    ? $"{value:0}M"
                    : $"{value:0.0}M";
            }

            return $"{coins / 1_000_000_000.0:0.0}B";
        }


        // =========================================================
        // WATER ANIMATIE
        // =========================================================

        private async Task AnimateButton(ImageButton button) { await button.ScaleTo(0.92, 200, Easing.CubicInOut); await button.ScaleTo(1.0, 200, Easing.CubicInOut); }

        private async Task PlayWaterDropAnimation(Flower flower)
        {
            Image waterDrop = new Image
            {
                Source = "waterdrop.gif",
                WidthRequest = 80,
                HeightRequest = 80,
                Aspect = Aspect.AspectFit,
                IsAnimationPlaying = true,
                Opacity = 1,
                TranslationY = -80,
                TranslationX = 0,
                ZIndex = flower.Position.Row == 1 ? 3 : 1
            };

            FlowerGrid.Add(
                waterDrop,
                flower.Position.Column,
                flower.Position.Row
            );

            IAudioPlayer player = AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync("water.mp3")
            );
            player.Volume = 0.3;
            player.Play();

            // Druppel valt naar beneden terwijl hij langzaam verdwijnt
            await Task.WhenAll(
                waterDrop.TranslateTo(0, 12, 1000, Easing.CubicOut),
                waterDrop.FadeTo(0, 1000, Easing.CubicIn)
            );

            waterDrop.IsAnimationPlaying = false;
            FlowerGrid.Children.Remove(waterDrop);
        }

        // =========================================================
        // GRASS
        // =========================================================

        private void CreateGrass()
        {
            GrassLayer.Children.Clear();
            GrassLayer.ColumnDefinitions.Clear();
            grassBlades.Clear();

            int grassCount = 30;

            for (int i = 0; i < grassCount; i++)
            {
                GrassLayer.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width = GridLength.Star
                    }
                );

                double grassHeight = grassRandom.Next(35, 70);

                double grassWidthScale =
                    0.65 + grassRandom.NextDouble() * 0.20;

                double horizontalScale =
                    grassRandom.Next(2) == 0
                        ? grassWidthScale
                        : -grassWidthScale;

                Image grass = new Image
                {
                    Source = "grass.png",

                    WidthRequest = 18,
                    HeightRequest = grassHeight,

                    Aspect = Aspect.Fill,

                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End,

                    AnchorX = 0.5,
                    AnchorY = 1.0,

                    InputTransparent = true,

                    // Kleine willekeurige afwijking,
                    // maar niet genoeg om grote gaten te maken
                    TranslationX = grassRandom.Next(-4, 3),

                    TranslationY = 3,

                    ScaleX = horizontalScale
                };

                GrassLayer.Add(grass, i, 0);
                grassBlades.Add(grass);
            }
        }


        private async Task AnimateGrassBlade(
    Image grass,
    CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(
                    grassRandom.Next(0, 1800),
                    cancellationToken
                );

                while (!cancellationToken.IsCancellationRequested)
                {
                    double windAngle =
                        4 + grassRandom.NextDouble() * 3;

                    double bounceAngle =
                        windAngle * (0.15 + grassRandom.NextDouble() * 0.25);

                    uint windDuration =
                        (uint)grassRandom.Next(600, 1000);

                    uint bounceDuration =
                        (uint)grassRandom.Next(400, 700);

                    uint returnDuration =
                        (uint)grassRandom.Next(500, 800);

                    await grass.RotateTo(
                        windAngle,
                        windDuration,
                        Easing.SinInOut
                    );

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await grass.RotateTo(
                        bounceAngle,
                        bounceDuration,
                        Easing.SinInOut
                    );

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await grass.RotateTo(
                        0,
                        returnDuration,
                        Easing.SinInOut
                    );

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await Task.Delay(
                        grassRandom.Next(100, 500),
                        cancellationToken
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // Normaal wanneer de grass animation wordt gestopt
            }
        }

        private void StartGrassAnimation()
        {
            StopGrassAnimation();

            grassAnimationCts = new CancellationTokenSource();

            foreach (Image grass in grassBlades)
            {
                _ = AnimateGrassBlade(
                    grass,
                    grassAnimationCts.Token
                );
            }
        }

        private void StopGrassAnimation()
        {
            if (grassAnimationCts != null)
            {
                grassAnimationCts.Cancel();
                grassAnimationCts.Dispose();
                grassAnimationCts = null;
            }
        }

        // =========================================================
        // OVERLAY/SHOP
        // =========================================================

        private async void ShopButton_Clicked(object sender, EventArgs e)
        {
            if (shopMode)
            {
                shopMode = false;

                ShopItemsLayout.Children.Clear();

                ShopLayout.IsVisible = false;
                ShopLayout.IsEnabled = false;

                StopShopMusic();
                return;
            }


            Inventory.IsVisible = false;
            Inventory.IsEnabled = false;
            InventoryLayout.Children.Clear();

            shopMode = true;

            ShopLayout.IsVisible = true;
            ShopLayout.IsEnabled = true;

            shopMusicPlayer = AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync("sans.mp3")
            );

            shopMusicPlayer.Volume = 0.3;
            shopMusicPlayer.Loop = true;
            shopMusicPlayer.Play();

            RefreshShop();
        }
        private void RefreshShop()
        {
            ShopItemsLayout.Children.Clear();

            foreach (FlowerPack pack in PackProvider.AllPacks)
            {
                // Alles van één pack komt in deze container.
                VerticalStackLayout packSection = new VerticalStackLayout
                {
                    Spacing = 0,
                    Padding = 0,
                    Margin = 0

                };

                // Odds-layout, standaard verborgen.
                VerticalStackLayout oddsLayout = new VerticalStackLayout
                {
                    IsVisible = false,
                    Spacing = 2,
                    Padding = new Thickness(30, 15, 15, 5),
                    
                };

                // Voeg de odds toe in de volgorde van FlowerRarity.
                foreach (FlowerRarity rarity in Enum.GetValues<FlowerRarity>())
                {
                    if (!pack.RarityWeights.TryGetValue(rarity, out double chance))
                        continue;

                    Label oddsLabel = new Label
                    {
                        Text = $"{rarity}: {chance:0.##}%",
                        FontFamily = "Minecraft",
                        FontSize = 15,
                        TextColor = RarityHelper.GetColor(rarity),
                        HorizontalOptions = LayoutOptions.Start,
                        FontAttributes = FontAttributes.Bold,

                                                Shadow = new Shadow
                                                {
                                                    Brush = Colors.White,
                                                    Radius = 5,
                                                    Opacity = 0.8f,
                                                    Offset = new Point(0, 0)
                                                }
                    };

                    oddsLayout.Add(oddsLabel);


                }

                Label cantAffordLabel = new Label
                {
                    Text = $"You need {pack.Price - coinAmount} more coins",
                    FontFamily = "Minecraft",
                    FontSize = 15,
                    TextColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Start,
                    FontAttributes = FontAttributes.Bold,
                    IsVisible = false,
                    Padding = new Thickness(30, 15, 15, 5),

                    Shadow = new Shadow
                    {
                        Brush = Colors.White,
                        Radius = 5,
                        Opacity = 0.8f,
                        Offset = new Point(0, 0)
                    }
                };

                cantAffordLabels[pack] = cantAffordLabel;

                // De bestaande pack-knop.
                Border packButton = new Border
                {
                    HeightRequest = 80,
                    Margin = new Thickness(5),
                    Padding = 5,
                    BackgroundColor = Colors.Transparent,
                    StrokeThickness = 0
                };

                Grid grid = new Grid
                {
                    ColumnDefinitions =
            {
                new ColumnDefinition { Width = 60 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 70 }
            }
                };

                ImageButton packImage = new ImageButton
                {
                    Source = pack.Png,
                    WidthRequest = 70,
                    HeightRequest = 70,
                    TranslationX = 10,
                    Aspect = Aspect.AspectFit
                };

                packImage.Clicked += async (sender, e) =>
                {
                    await AnimateButton((ImageButton)sender);

                    if (cantAffordLabels[pack].IsVisible)
                    {
                        cantAffordLabels[pack].IsVisible = false;
                        oddsLayout.IsVisible = true;
                        return;
                    }

                    oddsLayout.IsVisible = !oddsLayout.IsVisible;
                };

                grid.Add(packImage, 0, 0);

                VerticalStackLayout info = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    TranslationX = 10,
                };

                Label nameLabel = new Label
                {
                    Text = pack.Name,
                    FontFamily = "Minecraft",
                    FontSize = 16,
                    TextColor = Colors.White,

                                            Shadow = new Shadow
                                            {
                                                Brush = Colors.White,
                                                Radius = 5,
                                                Opacity = 0.8f,
                                                Offset = new Point(0, 0)
                                            }
                };

                Image stillcoin = new Image
                {
                    Source = "stillcoin.png",
                    HeightRequest = 25,
                    WidthRequest = 25,
                };

                Label priceLabel = new Label
                {
                    Text = $" {pack.Price}",
                    FontFamily = "Minecraft",
                    FontSize = 13,
                    TextColor = Colors.White,
                    TranslationY = 8,
                };

                HorizontalStackLayout infoGrid = new HorizontalStackLayout();

                infoGrid.Add(stillcoin);
                infoGrid.Add(priceLabel);
                info.Add(nameLabel);
                info.Add(infoGrid);

                grid.Add(info, 1, 0);

                Grid buyGrid = new Grid
                {
                    WidthRequest = 70,
                    HeightRequest = 60,
                    TranslationX = -10,
                };

                ImageButton buyButton = new ImageButton
                {
                    Source = "buybutton.png",
                    Aspect = Aspect.AspectFit


                };

                Label buytext = new Label
                {
                    Text = "BUY",
                    FontFamily = "Minecraft",
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    InputTransparent = true,

                                            Shadow = new Shadow
                                            {
                                                Brush = Colors.White,
                                                Radius = 5,
                                                Opacity = 0.8f,
                                                Offset = new Point(0, 0)
                                            }
                };

                buyGrid.Add(buyButton);
                buyGrid.Add(buytext);

                buyButton.Clicked += async (sender, e) =>
                {
                    await AnimateButton(buyButton);

                    if (coinAmount < pack.Price)
                    {
                        oddsLayout.IsVisible = false;
                        cantAffordLabels[pack].IsVisible = true;
                        return;
                    }

                    cantAffordLabels[pack].IsVisible = false;

                    await BuyPack(pack);
                };

                grid.Add(buyGrid, 2, 0);

                packButton.Content = grid;

                // Eerst odds, daarna de pack.
                packSection.Add(oddsLayout);
                packSection.Add(cantAffordLabel);
                packSection.Add(packButton);

                ShopItemsLayout.Children.Add(packSection);
            }
        }


        private async Task BuyPack(FlowerPack pack)
        {
            if (packOpeningInProgress)
                return;

            packOpeningInProgress = true;

            try
            {
                if (coinAmount < pack.Price)
                {
                    await DisplayAlert(
                        "Not enough coins",
                        $"You need {pack.Price} coins to buy this pack.",
                        "OK"
                    );

                    return;
                }

                coinAmount -= pack.Price;
                coinLabel.Text = FormatCoins(coinAmount);

                InventoryFlowerObject reward =
                    flowerPackService.OpenPack(pack);

                List<FlowerType> roll =
                    animationService.CreateRoll(
                        pack,
                        reward.Flowertype,
                        60
                    );

                await PlayRollAnimation(
                    roll,
                    reward.Flowertype
                );

                AddSeedToInventory(
                    reward.Flowertype,
                    reward.Quantity
                );

                await ShowFlowerWinOverlay(reward.Flowertype);

                await SaveGameDataAsync();

                RefreshShop();

                RefreshShop();
            }
            finally
            {
                packOpeningInProgress = false;
            }
        }


        // =========================================================
        // ROLL ANIMATION
        // =========================================================

        private async Task PlayRollAnimation(
            List<FlowerType> roll,
            FlowerType winningFlower)
        {
            const double slotWidth = 80;
            const double slotHeight = 100;

            const double imageSize = 60;
            const double spacing = 8;

            const double windowWidth = 350;
            const double windowHeight = 110;

            const double animationDuration = 7000;

            Grid overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#CC000000"),
                ZIndex = 9999,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            Grid window = new Grid
            {
                WidthRequest = windowWidth,
                HeightRequest = windowHeight,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                IsClippedToBounds = true
            };

            HorizontalStackLayout rollLayout = new HorizontalStackLayout
            {
                Spacing = spacing,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center
            };

            // =========================================================
            // FLOWER SLOTS
            // =========================================================


            foreach (FlowerType flower in roll)
            {

                Color rarityColor =
                RarityHelper.GetColor(flower.Rarity);

                Label nameLabel = new Label
                {
                    Text = flower.Name,
                    FontFamily = "Minecraft",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = rarityColor,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    HeightRequest = 25,

                        Shadow = new Shadow
                        {
                            Brush = Colors.White,
                            Radius = 5,
                            Opacity = 0.8f,
                            Offset = new Point(0, 0)
                        }
                };

                Image image = new Image
                {
                    Source = flower.Sprite,
                    WidthRequest = imageSize,
                    HeightRequest = imageSize,
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Center
                };

                VerticalStackLayout slotLayout = new VerticalStackLayout
                {
                    Spacing = 0,
                    Padding = 0,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                slotLayout.Children.Add(image);
                slotLayout.Children.Add(nameLabel);

                Border slot = new Border
                {
                    WidthRequest = slotWidth,
                    HeightRequest = slotHeight,
                    StrokeThickness = 2,
                    Stroke = Colors.White,
                    BackgroundColor = Color.FromArgb("#33000000"),
                    Padding = 2,
                    Content = slotLayout
                };

                rollLayout.Children.Add(slot);
            }

            window.Children.Add(rollLayout);

            // =========================================================
            // WINNING INDICATOR
            // =========================================================


            BoxView indicator = new BoxView
            {
                WidthRequest = 4,
                HeightRequest = slotHeight + 8,
                Color = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                ZIndex = 10
            };

            window.Children.Add(indicator);

            overlay.Children.Add(window);

            // =========================================================
            // OVERLAY BOVENOP DE BESTAANDE UI
            // =========================================================

            if (Content is not Grid hostGrid)
            {
                hostGrid = new Grid();

                if (Content is View currentContent)
                {
                    Content = null;
                    hostGrid.Children.Add(currentContent);
                }

                Content = hostGrid;
            }

            packAnimationOverlay = overlay;
            hostGrid.Children.Add(overlay);

            

            // =========================================================
            // WINNING POSITION
            // =========================================================

            if (roll.Count < 10)
            {
                hostGrid.Children.Remove(overlay);
                packAnimationOverlay = null;
                return;
            }


            int maximumWinningIndex = roll.Count - 8;

            int winningIndex = -1;

            for (int i = maximumWinningIndex; i >= 0; i--)
            {
                if (roll[i] == winningFlower)
                {
                    winningIndex = i;
                    break;
                }
            }

            if (winningIndex < 0)
            {
                hostGrid.Children.Remove(overlay);
                packAnimationOverlay = null;
                return;
            }

            // =========================================================
            // CALCULATE TARGET
            // =========================================================

            double step = slotWidth + spacing;

            double winningFlowerCenter =
                (winningIndex * step) +
                (slotWidth / 2.0);

            Random random = new Random();

            double randomFinalOffset;

            
            double roll2 = random.NextDouble();

            if (roll2 < 0.65)
            {
                
                randomFinalOffset = random.NextDouble() * 30 - 15;
            }
            else if (roll2 < 0.90)
            {
                
                randomFinalOffset = random.NextDouble() * 50 - 25;

                
                if (random.Next(2) == 0)
                    randomFinalOffset -= 10;
                else
                    randomFinalOffset += 10;
            }
            else
            {

                if (random.Next(2) == 0)
                    randomFinalOffset = -30 - random.NextDouble() * 8;
                else
                    randomFinalOffset = 30 + random.NextDouble() * 8;
            }

            double targetX =
                (windowWidth / 2.0)
                - winningFlowerCenter
                + randomFinalOffset;


            // =========================================================
            // PLAY ROLL
            // =========================================================

            StopShopMusic();

            IAudioPlayer player = AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync("wheelspin.mp3")
            );

            player.Volume = 0.2;
            player.Play();

            Easing suspenseEasing = new Easing(t =>
            {
                return 1 - Math.Pow(1 - t, 4);
            });

            await rollLayout.TranslateTo(
                targetX,
                0,
                (uint)animationDuration,
                suspenseEasing
            );

            
            StopShopMusic();

            

            shopMusicPlayer = AudioManager.Current.CreatePlayer(
                 await FileSystem.OpenAppPackageFileAsync("sans.mp3")
            );

            shopMusicPlayer.Volume = 0.3;
            shopMusicPlayer.Loop = true;
            shopMusicPlayer?.Play();

           

            await Task.Delay(700);


            // =========================================================
            // REMOVE OVERLAY
            // =========================================================

            hostGrid.Children.Remove(overlay);
            packAnimationOverlay = null;
        }

        private async Task ShowFlowerWinOverlay(FlowerType flower)
        {
            // =========================================================
            // FULLSCREEN OVERLAY
            // =========================================================

            Grid overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#CC000000"),
                ZIndex = 9999,

                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            // =========================================================
            // FLOWER WIN WINDOW
            // =========================================================

            Grid window = new Grid
            {
                WidthRequest = 300,
                HeightRequest = 400,

                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,

                ZIndex = 10000
            };

            // =========================================================
            // FLOWERWIN BACKGROUND
            // =========================================================

            Image background = new Image
            {
                Source = "flowerwin.png",

                WidthRequest = 300,
                HeightRequest = 200,

                Aspect = Aspect.Fill,

                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            window.Add(background);

            // =========================================================
            // CONTENT
            // =========================================================

            VerticalStackLayout content = new VerticalStackLayout
            {
                Spacing = 8,

                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,

                WidthRequest = 240
            };

            // =========================================================
            // FLOWER IMAGE
            // =========================================================

            Image flowerImage = new Image
            {
                Source = flower.Sprite,

                WidthRequest = 120,
                HeightRequest = 120,

                Aspect = Aspect.AspectFit,

                HorizontalOptions = LayoutOptions.Center
            };

            // =========================================================
            // FLOWER NAME
            // =========================================================

            Label flowerName = new Label
            {
                Text = flower.Name,

                FontFamily = "Minecraft",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,

                TextColor = Colors.White,

                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center
            };

            // =========================================================
            // RARITY
            // =========================================================

            Color rarityColor =
                RarityHelper.GetColor(flower.Rarity);

            Label rarityLabel = new Label
            {
                Text = flower.Rarity.ToString().ToUpper(),

                FontFamily = "Minecraft",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,

                TextColor = rarityColor,

                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,

                                Shadow = new Shadow
                                {
                                    Brush = Colors.White,
                                    Radius = 5,
                                    Opacity = 0.8f,
                                    Offset = new Point(0, 0)
                                }
            };

            // =========================================================
            // OK BUTTON
            // =========================================================

            Button okButton = new Button
            {
                Text = "OK",

                FontFamily = "Minecraft",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,

                TextColor = Colors.White,

                WidthRequest = 120,
                HeightRequest = 50,

                HorizontalOptions = LayoutOptions.Center,

                BackgroundColor = Color.FromArgb("#5A8F3D")
            };

            // =========================================================
            // CONTENT TOEVOEGEN
            // =========================================================

            content.Add(flowerImage);
            content.Add(flowerName);
            content.Add(rarityLabel);
            content.Add(okButton);

            window.Add(content);
            overlay.Add(window);

            // =========================================================
            // OVERLAY OP DE GAME ZETTEN
            // =========================================================

            if (Content is not Grid hostGrid)
            {
                hostGrid = new Grid();

                if (Content is View currentContent)
                {
                    Content = null;
                    hostGrid.Children.Add(currentContent);
                }

                Content = hostGrid;
            }

            hostGrid.Children.Add(overlay);

            // =========================================================
            // FLOATING ANIMATIES
            // =========================================================

            CancellationTokenSource animationCts =
                new CancellationTokenSource();

            Task flowerAnimation =
                FloatFlower(flowerImage, animationCts.Token);

            // =========================================================
            // WACHTEN OP OK
            // =========================================================

            TaskCompletionSource<bool> completion =
                new TaskCompletionSource<bool>(); 

            okButton.Clicked += (sender, e) =>
            {
                completion.TrySetResult(true);
            };

            await completion.Task;

            // Animaties stoppen
            animationCts.Cancel();

            try
            {
                await Task.WhenAll(
                    flowerAnimation
                    
                );
            }
            catch (OperationCanceledException)
            {
                // Normaal wanneer de popup gesloten wordt
            }

            // =========================================================
            // OVERLAY VERWIJDEREN
            // =========================================================

            hostGrid.Children.Remove(overlay);
        }

        // =========================================================
        // FLOWER FLOATING ANIMATION
        // =========================================================

        private async Task FloatFlower(
            Image flowerImage,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Langzaam omhoog
                await flowerImage.TranslateTo(
                    0,
                    -5,
                    800,
                    Easing.SinInOut
                );

                if (cancellationToken.IsCancellationRequested)
                    break;

                // Langzaam omlaag
                await flowerImage.TranslateTo(
                    0,
                    5,
                    800,
                    Easing.SinInOut
                );
            }

        }


        // =========================================================
        // TIMER
        // =========================================================

        private void StopGrowthTimer()
        {
            if (growthTimer != null)
            {
                growthTimer.Stop();

                growthTimer = null;
            }
        }



        private void StartGrowthTimer()
        {
            StopGrowthTimer();


            growthTimer =
                Dispatcher.CreateTimer();


            growthTimer.Interval =
                TimeSpan.FromSeconds(1);


            growthTimer.Tick +=
                (sender, e) =>
                {
                    if (activeFlower == null)
                    {
                        StopGrowthTimer();
                        return;
                    }


                    UpdateGrowthTimer();
                };


            growthTimer.Start();
        }

        private void StartFlowerPotCheckTimer()
        {
            if (flowerPotCheckTimer != null)
                flowerPotCheckTimer.Stop();

            flowerPotCheckTimer = Dispatcher.CreateTimer();

            flowerPotCheckTimer.Interval =
                TimeSpan.FromSeconds(1);

            flowerPotCheckTimer.Tick += (sender, e) =>
            {
                CheckFlowerPotForReadyFlowers();
            };

            flowerPotCheckTimer.Start();
        }

        private bool hadReadyFlower = false;

        private void CheckFlowerPotForReadyFlowers()
        {
            bool hasReadyFlower = false;

            foreach (Flower flower in flowerPotContent)
            {
                if (flower.WateredAt != null &&
                    flowerMoneyService.IsReadyToCollect(flower))
                {
                    hasReadyFlower = true;
                    break;
                }
            }

            
            if (hasReadyFlower && !hadReadyFlower)
            {
                hadReadyFlower = true;
                LoadFlowerpotContent();
            }

            if (!hasReadyFlower)
            {
                hadReadyFlower = false;
            }
        }

        // =========================================================
        // WIDGET
        // =========================================================

        private async Task<byte[]> CaptureFlowerPotAsync()
        {
            if (FlowerPot == null)
                return null;

            
            List<View> hiddenCoins = new List<View>();

            List<View> hiddenViews = new List<View>();

            foreach (View child in FlowerGrid.Children)
            {
                if (child is Image image &&
                    image.Source?.ToString().Contains("coingif.gif") == true)
                {
                    hiddenViews.Add(child);
                    child.IsVisible = false;
                }

                if (child is ImageButton button &&
                    (button.Source?.ToString().Contains("shovelselect.png") == true ||
                     button.Source?.ToString().Contains("waterselect.png") == true))
                {
                    hiddenViews.Add(child);
                    child.IsVisible = false;
                }
            }


            IScreenshotResult screenshot =
                await FlowerPot.CaptureAsync();


            foreach (View view in hiddenViews)
            {
                view.IsVisible = true;
            }

            using MemoryStream memoryStream =
                new MemoryStream();

            await screenshot.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }


        private async Task SaveFlowerPotSnapshotAsync()
        {
            byte[] imageBytes =
                await CaptureFlowerPotAsync();


            if (imageBytes == null)
                return;


#if ANDROID
            string path = Path.Combine(
                Android.App.Application.Context.FilesDir.AbsolutePath,
                "flowerpot_snapshot.png"
            );
#else
            string path = Path.Combine(
                FileSystem.AppDataDirectory,
                "flowerpot_snapshot.png"
            );
#endif


            await File.WriteAllBytesAsync(
                path,
                imageBytes
            );
        }


#if ANDROID

        private void UpdateFlowerPotWidget()
        {
            Android.Content.Context context =
                Android.App.Application.Context;


            Android.Appwidget.AppWidgetManager manager =
                Android.Appwidget.AppWidgetManager.GetInstance(
                    context
                );


            Android.Content.ComponentName componentName =
                new Android.Content.ComponentName(
                    context,
                    Java.Lang.Class.FromType(
                        typeof(MyWidgetReceiver)
                    )
                );


            int[] widgetIds =
                manager.GetAppWidgetIds(
                    componentName
                );


            foreach (int widgetId in widgetIds)
            {
                MyWidgetReceiver.UpdateWidget(
                    context,
                    manager,
                    widgetId
                );
            }
        }

#endif

    }
}