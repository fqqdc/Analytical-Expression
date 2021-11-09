string expression = "";

Dictionary<char, int> priority = new() 
{ 
    { '+', 0 },
    { '-', 0 },
    { '*', 1 },
    { '\\', 1 },
    { '(', 4 },
    { ')', -1 },
};