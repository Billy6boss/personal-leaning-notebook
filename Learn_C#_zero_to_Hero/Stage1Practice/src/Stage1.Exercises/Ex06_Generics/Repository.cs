namespace Stage1.Exercises.Ex06_Generics;

public interface IHasId
{
    int Id { get; }
}

/// <summary>
/// Stage 1 主題：Generics（泛型）。
///
/// 任務：
/// 實作一個限定為實作 IHasId 的型別使用的 generic in-memory repository。
///
/// 需求：
/// - Add(T item)：把 item 以 item.Id 為 key 儲存起來。如果已經存在
///   相同 Id 的項目，要拋出 ArgumentException。
/// - GetById(int id)：回傳符合的項目，找不到則回傳 null。
///   （在這裡 T 實務上會是 reference type，但請用 generic 的方式撰寫 —
///   想想看要用什麼 constraint 或 return type 才能表達「找不到」這件事。）
/// - GetAll()：把所有儲存的項目以 read-only list 回傳。
/// - Remove(int id)：如果存在該 Id 的項目就移除並回傳 true；
///   如果不存在則回傳 false。
/// </summary>
public class Repository<T> where T : class, IHasId
{
    public void Add(T item)
    {
        var isExist = this.GetById(item.Id) != null;
        if (isExist)
        {
            throw new ArgumentException($" item {item.Id} already exists");
        }
    }

    public T? GetById(int id)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<T> GetAll()
    {
        throw new NotImplementedException();
    }

    public bool Remove(int id)
    {
        throw new NotImplementedException();
    }
}
