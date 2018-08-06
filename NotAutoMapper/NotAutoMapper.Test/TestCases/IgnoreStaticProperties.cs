// FixLocation: Ln 3, Col 19
// Message: Map method from 'Human' to 'Monkey' can be completed

#region Models
public class Human
{
    public static Human Default { get; } = new Human("Default", 100);

    public Human(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; }
    public int Age { get; }
}

public class Monkey
{
    public static Monkey Default { get; } = new Monkey("Default", 100);

    public Monkey(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; }
    public int Age { get; }
}
#endregion

#region Input
public class MyMapper
{
    public Monkey Map(Human dto)
    { }
}
#endregion

#region Input
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
        (
            name: dto.Name,
            age: dto.Age
        );
    }
}
#endregion
