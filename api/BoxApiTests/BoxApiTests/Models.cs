﻿namespace DefaultNamespace;

public class Box
{
    public int Id { get; set; }
    public string Size { get; set; }
    public int Weight { get; set; }
    public int Price { get; set; }
    public string Material { get; set; }
    public string Color { get; set; }
    public int Quantity { get; set; }

}

public class InStockBoxes
{
    public int Id { get; set; }
    
    public int Weight { get; set; }
    
    public int Quantity { get; set; }
}