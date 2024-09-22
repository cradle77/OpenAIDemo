using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace OpenAIDemo.Server.Plugins
{
    public class ShoppingListPlugin
    {
        public List<ShoppingListItem> Items { get; } = new List<ShoppingListItem>();

        [KernelFunction("get_shopping_list")]
        [Description("This function returns the current content of the shopping list.")]
        public List<ShoppingListItem> GetShoppingList()
        {
            return this.Items;
        }

        [KernelFunction("add_shopping_list_item")]
        [Description("This function allows the management of a shopping list, and allows the user to add an item to his current shopping list. It returns the current content of the shopping list. If the user asks to add an item, and the item is already in the shopping list, this should result in modify_shopping_list_item to be called instead with an updated quantity.")]
        public List<ShoppingListItem> AddShoppingListItem(ShoppingListItem item)
        {
            if (item.Quantity == 0)
            {
                item.Quantity = 1;
            }

            this.Items.Add(item);
            return this.Items;
        }

        [KernelFunction("modify_shopping_list_item")]
        [Description("This function allows to modify or remove an item from the shopping list. The description field must be exactly the same as one of the this.Items in the shopping list. The quantity field must be set to 0 in case of removal. It returns the current content of the shopping list. If the user asks to add an item, and the item is already in the shopping list, this should result in modify to be called instead with an updated quantity.")]
        public List<ShoppingListItem> ModifyShoppingListItem(ShoppingListItem item)
        {
            if (item.Quantity == 0)
            {
                this.Items.RemoveAll(x => x.Description == item.Description);
            }
            else
            {
                var original = this.Items.FirstOrDefault(x => x.Description == item.Description);
                if (original == null)
                {
                    throw new InvalidOperationException($"Item {item.Description} not found in the shopping list");
                }
                original.Quantity = item.Quantity;
            }

            return this.Items;
        }
    }

    public class ShoppingListItem
    {
        [Description("The item that needs to be added to the list")]
        public string Description { get; set; }

        [Description("The quantity that needs to be purchased, if not provided set it to 1. Setting it to 0 removes the item.")]
        public int Quantity { get; set; }
    }
}
