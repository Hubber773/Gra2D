using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Gra2D
{
    public partial class InventoryWindow : Window
    {
        private readonly MainWindow mainWindow;
        private Rectangle? selectedRect;
        private int? selectedSlotIndex;
        private DateTime lastClickTime;
        private const double DoubleClickTime = 300;

        private Dictionary<int, string> hotbarKeyMap = new Dictionary<int, string>
        {
            { 1, "INV_I_stuffes1" },
            { 2, "INV_I_stuffes2" },
            { 3, "INV_I_stuffes3" },
            { 4, "INV_I_toolbar1" },
            { 5, "INV_I_toolbar2" },
            { 6, "INV_I_armorbar1" },
            { 7, "INV_I_abilitybar1" }
        };

        public InventoryWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            UpdateInventoryUI();
            InitializeInventorySlots();
            InitializeHotbarSlots();
            this.KeyDown += InventoryWindow_KeyDown;
            selectedRect = null;
        }

        private void InitializeInventorySlots()
        {
            for (int i = 1; i <= 52; i++)
            {
                string imageName = $"INV_I_hotbarinventory{i}";
                Image image = (Image)this.FindName(imageName);
                if (image != null)
                {
                    image.MouseLeftButtonDown += InventorySlot_Click;
                }
            }
        }

        private void InitializeHotbarSlots()
        {
            foreach (var slotName in hotbarKeyMap.Values)
            {
                Image image = (Image)this.FindName(slotName);
                if (image != null)
                {
                    image.MouseLeftButtonDown += HotbarSlot_Click;
                }
            }
        }

        private void InventorySlot_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image)
            {
                string imageName = image.Name;
                int slotNumber = int.Parse(imageName.Replace("INV_I_hotbarinventory", ""));
                string rectName = $"INV_RA_hotbarinventory{slotNumber}";
                Rectangle rect = (Rectangle)this.FindName(rectName);
                if (rect != null)
                {
                    if (selectedSlotIndex == slotNumber)
                    {
                        rect.Stroke = Brushes.Black;
                        selectedSlotIndex = null;
                        selectedRect = null;
                    }
                    else
                    {
                        if (selectedRect != null)
                        {
                            selectedRect.Stroke = Brushes.Black;
                        }
                        rect.Stroke = Brushes.Yellow;
                        selectedRect = rect;
                        selectedSlotIndex = slotNumber;
                    }
                }
            }
        }

        private void HotbarSlot_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image)
            {
                string imageName = image.Name;
                if ((DateTime.Now - lastClickTime).TotalMilliseconds < DoubleClickTime)
                {
                    UnequipItem(imageName);
                }
                lastClickTime = DateTime.Now;
            }
        }

        private void InventoryWindow_KeyDown(object sender, KeyEventArgs e)
        {
            int keyNum = -1;
            if (e.Key >= Key.D1 && e.Key <= Key.D7)
            {
                keyNum = e.Key - Key.D0;
            }
            else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad7)
            {
                keyNum = e.Key - Key.NumPad0;
            }

            if (keyNum != -1 && selectedSlotIndex.HasValue)
            {
                MoveItemToHotbar(keyNum);
                e.Handled = true;
            }
        }

        private bool CanEquipToSlot(ItemType item, int slotNum)
        {
            if (item == ItemType.Wood)
            {
                return false;
            }
            else if (item == ItemType.Bomb)
            {
                return slotNum >= 1 && slotNum <= 3;
            }
            return false;
        }

        private void MoveItemToHotbar(int hotbarSlot)
        {
            if (!hotbarKeyMap.ContainsKey(hotbarSlot) || !selectedSlotIndex.HasValue) return;

            string targetSlotName = hotbarKeyMap[hotbarSlot];
            string mainSlotName = targetSlotName.Replace("INV_", "");
            Image targetImage = (Image)this.FindName(targetSlotName);
            if (targetImage == null) return;

            var inventoryItems = mainWindow.GetInventoryItems();
            int index = selectedSlotIndex.Value - 1;
            if (index >= inventoryItems.Count) return;

            ItemType selectedItem = inventoryItems[index];

            if (!CanEquipToSlot(selectedItem, hotbarSlot))
            {
                return;
            }

            var mainHotbarItems = mainWindow.GetHotbarItems();
            if (mainHotbarItems.ContainsKey(mainSlotName) && mainHotbarItems[mainSlotName].HasValue)
            {
                ItemType hotbarItem = mainHotbarItems[mainSlotName].Value;
                mainWindow.UpdateHotbarItem(mainSlotName, selectedItem);
                inventoryItems[index] = hotbarItem;
            }
            else
            {
                mainWindow.UpdateHotbarItem(mainSlotName, selectedItem);
                inventoryItems.RemoveAt(index);
            }

            targetImage.Source = new BitmapImage(new Uri(GetImageForItem(selectedItem), UriKind.Relative));
            UpdateInventoryUI();
            mainWindow.UpdateHotbarUI();

            if (selectedRect != null)
            {
                selectedRect.Stroke = Brushes.Black;
            }
            selectedSlotIndex = null;
            selectedRect = null;
        }

        private void UnequipItem(string hotbarSlotName)
        {
            string mainSlotName = hotbarSlotName.Replace("INV_", "");
            var mainHotbarItems = mainWindow.GetHotbarItems();
            if (!mainHotbarItems.ContainsKey(mainSlotName) || !mainHotbarItems[mainSlotName].HasValue) return;

            var inventoryItems = mainWindow.GetInventoryItems();
            if (inventoryItems.Count >= 52)
            {
                return;
            }

            ItemType itemToUnequip = mainHotbarItems[mainSlotName].Value;
            inventoryItems.Add(itemToUnequip);
            mainWindow.UpdateHotbarItem(mainSlotName, null);

            Image hotbarImage = (Image)this.FindName(hotbarSlotName);
            if (hotbarImage != null)
            {
                hotbarImage.Source = null;
            }
            UpdateInventoryUI();
            mainWindow.UpdateHotbarUI();
        }

        public void UpdateInventoryUI()
        {
            var items = mainWindow.GetInventoryItems();
            for (int i = 1; i <= 52; i++)
            {
                string imageName = $"INV_I_hotbarinventory{i}";
                Image image = (Image)this.FindName(imageName);
                if (image != null)
                {
                    if (i <= items.Count)
                    {
                        image.Source = new BitmapImage(new Uri(GetImageForItem(items[i - 1]), UriKind.Relative));
                    }
                    else
                    {
                        image.Source = null;
                    }
                }
            }

            var mainHotbarItems = mainWindow.GetHotbarItems();
            foreach (var slot in hotbarKeyMap.Values)
            {
                string mainSlotName = slot.Replace("INV_", "");
                Image hotbarImage = (Image)this.FindName(slot);
                if (hotbarImage != null)
                {
                    ItemType? item = mainHotbarItems.ContainsKey(mainSlotName) ? mainHotbarItems[mainSlotName] : null;
                    hotbarImage.Source = item.HasValue
                        ? new BitmapImage(new Uri(GetImageForItem(item.Value), UriKind.Relative))
                        : null;
                }
            }
        }

        private string GetImageForItem(ItemType item)
        {
            switch (item)
            {
                case ItemType.Wood:
                    return "inv_drewno.png";
                case ItemType.Bomb:
                    return "inv_bomb.png";
                default:
                    return "";
            }
        }
    }
}