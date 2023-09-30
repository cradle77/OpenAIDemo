using Azure.AI.OpenAI;
using System.Text.Json;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public class ShoppingListItem
    {
        public string Description { get; set; }
        public int Quantity { get; set; }
    }

    public class ShoppingList 
    {
        public List<ShoppingListItem> Items { get; } = new List<ShoppingListItem>();

        public static readonly ShoppingList Instance = new ShoppingList();
    }

    public class ShoppingAddAdapter : IFunctionAdapter
    {
        public string FunctionName => "add-shopping-list-item";

        public FunctionDefinition GetFunctionDefinition()
        {
            return new FunctionDefinition()
            {
                Name = this.FunctionName,
                Description = "This function allows the management of a shopping list, and allows the user to add an item to his current shopping list. It returns the current content of the shopping list. If the user asks to add an item, and the item is already in the shopping list, this should result in modify-shopping-list-item to be called instead with an updated quantity.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        Description = new
                        {
                            Type = "string",
                            Description = "The item that needs to be added to the list",
                        },
                        Quantity = new
                        {
                            Type = "number",
                            Description = "The quantity that needs to be purchased, if not provided set it to 1"
                        }
                    },
                    Required = new[] { "Item", "Quantity" },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        public async Task<ChatMessage> InvokeAsync(string arguments)
        {
            var todo = JsonSerializer.Deserialize<ShoppingListItem>(arguments, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (todo.Quantity == 0)
            {
                todo.Quantity = 1;
            }

            ShoppingList.Instance.Items.Add(todo);

            return new ChatMessage()
            {
                Role = ChatRole.Function,
                Name = this.FunctionName,
                Content = JsonSerializer.Serialize(ShoppingList.Instance.Items, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }
    }

    public class ShoppingGetListAdapter : IFunctionAdapter
    {
        public string FunctionName => "get-shopping-list";

        public FunctionDefinition GetFunctionDefinition()
        {
            return new FunctionDefinition()
            {
                Name = this.FunctionName,
                Description = "This function returns the most updated content of the shopping list in a JSON array format",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        Dummy = new
                        {
                            Type = "string",
                            Description = "this is not used",
                        }
                    },
                    Required = new string[] {  },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        public async Task<ChatMessage> InvokeAsync(string arguments)
        {
            return new ChatMessage() 
            {
                Role = ChatRole.Function,
                Name = this.FunctionName,
                Content = JsonSerializer.Serialize(ShoppingList.Instance.Items, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }
    }

    public class ShoppingModifyAdapter : IFunctionAdapter
    {
        public string FunctionName => "modify-shopping-list-item";

        public FunctionDefinition GetFunctionDefinition()
        {
            return new FunctionDefinition()
            {
                Name = this.FunctionName,
                Description = "This function allows to modify or remove an item from the shopping list. The description field must be exactly the same as one of the items in the shopping list. The quantity field must be set to 0 in case of removal. It returns the current content of the shopping list. If the user asks to add an item, and the item is already in the shopping list, this should result in modify to be called instead with an updated quantity.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        Description = new
                        {
                            Type = "string",
                            Description = "The item that needs to be added to the list",
                        },
                        Quantity = new
                        {
                            Type = "number",
                            Description = "The quantity that needs to be purchased, if not provided set it to 1"
                        }
                    },
                    Required = new[] { "Item", "Quantity" },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        public async Task<ChatMessage> InvokeAsync(string arguments)
        {
            var todo = JsonSerializer.Deserialize<ShoppingListItem>(arguments, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (todo.Quantity == 0)
            {
                ShoppingList.Instance.Items.RemoveAll(x => x.Description == todo.Description);
            }
            else
            { 
                var item = ShoppingList.Instance.Items.FirstOrDefault(x => x.Description == todo.Description);
                if (item == null)
                {
                    return new ChatMessage(ChatRole.Function, $"Item {todo.Description} not found") { Name = this.FunctionName };
                }
                item.Quantity = todo.Quantity;
            }

            return new ChatMessage()
            {
                Role = ChatRole.Function,
                Name = this.FunctionName,
                Content = JsonSerializer.Serialize(ShoppingList.Instance.Items, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }
    }
}
