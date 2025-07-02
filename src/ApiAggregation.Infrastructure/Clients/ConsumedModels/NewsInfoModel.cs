namespace ApiAggregation.Infrastructure.Clients.ConsumedModels;

public class NewsInfoModel
{
    public IReadOnlyList<Article> Articles { get; }

    public NewsInfoModel(IReadOnlyList<Article> articles)
    {
        Articles = articles;
    }

    public IReadOnlyList<Article> GetArticles()
    {
        return Articles;
    }
}

public class Article
{
    public string Title { get; }

    public Article(string title)
    {
        Title = title;
    }
}