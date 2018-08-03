// CURSOR: Ln 2, Col 17
// CURSOR: Ln 2, Col 18
// CURSOR: Ln 2, Col 18

#region Models
public class Human
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class Monkey
{
    public string Name { get; set; }
    public int Age { get; set; }
}
#endregion

#region Input
// CURSOR: Ln 2, Col 17
// CURSOR: Ln 2, Col 18
public class MyMapper
{
    public Monkey Map(Human dto)
}
#endregion

#region Input
// CURSOR: Ln 2, Col 17
public class MyMapper
{
    public Monkey Map(Human dto)
    {

    }
}
#endregion

#region Expected
public class MyMapper
{
    public Monkey Map(Human dto)
    {
        return new Monkey
        {
            Name = dto.Name,
            Age = dto.Age
        };
    }
}
#endregion
