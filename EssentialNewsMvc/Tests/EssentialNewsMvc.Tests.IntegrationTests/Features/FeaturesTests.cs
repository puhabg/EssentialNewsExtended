﻿using Autofac;
using Autofac.Integration.Mvc;
using EssentialNewsMvc.Data;
using EssentialNewsMvc.Data.Models;
using EssentialNewsMvc.Infrastructure.Mappings;
using EssentialNewsMvc.Web.App_Start;
using EssentialNewsMvc.Web.Areas.Administration.Models.Grid;
using EssentialNewsMvc.Web.Features.AdministrationArticles;
using EssentialNewsMvc.Web.Features.News;
using EssentialNewsMvc.Web.Features.NewsArticles;
using EssentialNewsMvc.Web.ViewModels.Account;
using EssentialNewsMvc.Web.ViewModels.News;
using EssentialNewsMvc.Web.ViewModels.Partials;
using MediatR;
using NUnit.Framework;
using Respawn;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace EssentialNewsMvc.Tests.IntegrationTests.Features
{
    [TestFixture]
    public class FeaturesTests
    {
        private static IContainer container;
        private IDependencyResolver originalResolver = null;
        private ILifetimeScopeProvider scopeProvider = null;
        private static Checkpoint checkpoint = new Checkpoint();

        [SetUp]
        public void Init()
        {
            Assembly asm = typeof(LoginViewModel).Assembly;
            var autoMapperConfig = new AutoMapperConfig();
            autoMapperConfig.Execute(asm);
            AutofacConfig.RegisterAutofac();
            container = AutofacConfig.Container;
            this.scopeProvider = new SimpleLifetimeScopeProvider(container);
            var resolver = new AutofacDependencyResolver(container, this.scopeProvider);
            this.originalResolver = DependencyResolver.Current;
            DependencyResolver.SetResolver(resolver);
        }

        [TearDown]
        public void Cleanup()
        {
            this.scopeProvider.EndLifetimeScope();
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString();
            checkpoint.Reset(connectionString);
        }

        [Test]
        public void AsideArticleHandlerShould_GetArticles()
        {

            var handler = DependencyResolver.Current.GetService<IRequestHandler<AsideArticlesQuery, AsideViewModel>>();
            var dbContext = DependencyResolver.Current.GetService<ApplicationDbContext>();
            var article = new NewsArticle()
            {
                Title = "SomeTitle",
                Content = "SomeContent",
                SampleContent = "SomeSampleContent"
            };
            dbContext.NewsArticles.Add(article);
            dbContext.SaveChanges();

            var result = handler.Handle(new AsideArticlesQuery());
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf(typeof(AsideViewModel)), "Wrong instance");
                Assert.That(result.MostVisitedArticles.First().Title, Is.EqualTo(article.Title), "Wrong Title");
            });
        }

        [Test]
        public void DeleteArticleCommandHandlerShould_DeleteArticle()
        {

            var handler = DependencyResolver.Current.GetService<IRequestHandler<DeleteArticleCommand, string>>();
            var dbContext = DependencyResolver.Current.GetService<ApplicationDbContext>();
            var article = new NewsArticle()
            {
                Title = "SomeTitle",
                Content = "SomeContent",
                SampleContent = "SomeSampleContent"
            };
            var addedArticle = dbContext.NewsArticles.Add(article);
            dbContext.SaveChanges();

            var result = handler.Handle(new DeleteArticleCommand() { Id = addedArticle.Id.ToString() });
            var dbArticle = dbContext.NewsArticles.Find(addedArticle.Id);

           Assert.That(dbArticle.IsDeleted, Is.EqualTo(true));
        }

        [Test]
        public void RegionsCategoriesQueryHandlerShould_GetRegionsAndCategories()
        {

            var handler = DependencyResolver.Current.GetService<IRequestHandler<CategoriesRegionsQuery, CreateNewsViewModel>>();
            var dbContext = DependencyResolver.Current.GetService<ApplicationDbContext>();
            var regions = new List<Region>()
            {
                new Region() { Name = "Some" },
                new Region() { Name = "Other" }
            };
            var categories = new List<NewsCategory>()
            {
                new NewsCategory() { Name = "Some" },
                new NewsCategory() { Name = "Other" }
            };
            foreach (var region in regions)
            {
                dbContext.Regions.Add(region);
            }

            foreach (var category in categories)
            {
                dbContext.Categories.Add(category);
            }

            dbContext.SaveChanges();

            var result = handler.Handle(new CategoriesRegionsQuery());
            Assert.Multiple(() => {
                Assert.That(result.Regions.Count(), Is.EqualTo(2), "Number of regions");
                Assert.That(result.NewsCategories.Count(), Is.EqualTo(2), "Number of categories");
            });
        }

        [Test]
        public void EditArticleCommandHandlerShould_EditArticle()
        {

            var handler = DependencyResolver.Current.GetService<IRequestHandler<EditArticleCommand, string>>();
            var dbContext = DependencyResolver.Current.GetService<ApplicationDbContext>();
            var article = new NewsArticle()
            {
                Title = "SomeTitle",
                Content = "SomeContent",
                SampleContent = "SomeSampleContent"
            };
            var addedArticle = dbContext.NewsArticles.Add(article);
            dbContext.SaveChanges();
            dbContext.SaveChanges();
            var model = new GridArticleViewModel()
            {
                Title = "New title",
                Id = addedArticle.Id,
                Content = "Some content",
                IsDeleted = false,
                CreatedOn = DateTime.Now
            };
            var result = handler.Handle(new EditArticleCommand() { Model = model });
            var dbArticle = dbContext.NewsArticles.Find(addedArticle.Id);
            
            Assert.That(dbArticle.Title, Is.EqualTo("New title"));
        }

        [Test]
        public void ArticleGridQueryHandlerShould_GetArticles()
        {

            var handler = DependencyResolver.Current.GetService<IRequestHandler<ArticlesGridQuery, List<GridArticleViewModel>>>();
            var dbContext = DependencyResolver.Current.GetService<ApplicationDbContext>();
            var article = new NewsArticle()
            {
                Title = "SomeTitle",
                Content = "SomeContent",
                SampleContent = "SomeSampleContent"
            };
            var addedArticle = dbContext.NewsArticles.Add(article);
            dbContext.SaveChanges();
            dbContext.SaveChanges();
            var model = new GridArticleViewModel()
            {
                Title = "New title",
                Id = addedArticle.Id,
                Content = "Some content",
                IsDeleted = false,
                CreatedOn = DateTime.Now
            };
            var result = handler.Handle(new ArticlesGridQuery() { Sidx = "", Sort = "", Page = 1, Row = 2 });
            
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void NewsDetailsQueryHandlerShould_GetArticles()
        {

            var handler = DependencyResolver.Current.GetService<IRequestHandler<NewsDetailsQuery, DetailsViewModel>>();
            var dbContext = DependencyResolver.Current.GetService<ApplicationDbContext>();
            var article = new NewsArticle()
            {
                Title = "SomeTitle",
                Content = "SomeContent",
                SampleContent = "SomeSampleContent"
            };
            var addedArticle = dbContext.NewsArticles.Add(article);
            dbContext.SaveChanges();
            dbContext.SaveChanges();
            
            var result = handler.Handle(new NewsDetailsQuery() { Id = addedArticle.Id });

            Assert.That(result.Title, Is.EqualTo(article.Title));
        }

        //[Test]
        //public void CreateAtricleCommandHandlerShould_CreateArticles()
        //{
        //    URL url = Thread.currentThread().getContextClassLoader().getResource("mypackage/YourFile.csv");
        //    File file = new File(url.getPath());

        //    var handler = DependencyResolver.Current.GetService<IRequestHandler<CreateArticleCommand>>();
        //    var mediator = DependencyResolver.Current.GetService<IMediator>();
        //    var dbContext = DependencyResolver.Current.GetService<ApplicationDbContext>();
        //    var roleStore = new RoleStore<IdentityRole>(dbContext);
        //    var roleManager = new RoleManager<IdentityRole>(roleStore);
        //    var userStore = new UserStore<ApplicationUser>(dbContext);
        //    var userManager = new UserManager<ApplicationUser>(userStore);
        //    var journalistRole = new IdentityRole("Journalist");
        //    roleManager.Create(journalistRole);
        //    string journalistUserName = "journalist";
        //    string journalistEmail = "journalist@admin.bg";
        //    string journalistPassword = "Password1";
        //    var newUser = new ApplicationUser()
        //    {
        //        UserName = journalistUserName,
        //        Email = journalistEmail,
        //        PhoneNumber = "5551234567",
        //    };
        //    userManager.Create(newUser, journalistPassword);
        //    userManager.SetLockoutEnabled(newUser.Id, false);
        //    userManager.AddToRole(newUser.Id, "Journalist");
        //    var model = new CreateNewsViewModel()
        //    {
        //        Title = "Some Title",
        //        Content = "Some Content",
        //        IsTop = true,
        //        NewsCategoryId = 1,
        //        RegionId = 1,
        //        SampleContent = "Some sample content",
        //        Upload = mockFile.Object,
        //    };

        //    var result = mediator.Send(new CreateArticleCommand() { Model = model, UserId = newUser.Id });
        //    result.Wait();

        //    var articleCount = dbContext.NewsArticles.Count();

        //    Assert.That(articleCount, Is.EqualTo(1));
        //}

    }
}
