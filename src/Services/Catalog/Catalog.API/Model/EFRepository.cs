using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.eShopOnContainers.Services.Catalog.API.Model;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using Catalog.API.Extensions;
using Microsoft.eShopOnContainers.Services.Catalog.API.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Model
{
    public class EFRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly CatalogContext _dbContext;

        public EFRepository(CatalogContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbContext.Set<T>().SingleOrDefaultAsync(e => e.Id == id);
        }

        public async Task<T> GetSingleBySpecAsync(ISpecification<T> spec)
        {
            return (await ListAsync(spec)).FirstOrDefault();
        }

        public async Task<List<T>> ListAllAsync()
        {
            return await _dbContext.Set<T>().AsQueryable<T>().ToListAsync();
        }

        public async Task<T> AddAsync(T entity, bool saveChanges = true)
        {
            await _dbContext.Set<T>().AddAsync(entity);
            if (saveChanges)
            {
                await _dbContext.SaveChangesAsync();
            }

            return entity;
        }

        public async Task<T> UpdateAsync(T entity, bool saveChanges)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            if (saveChanges)
            {
                await _dbContext.SaveChangesAsync();
            }

            return entity;
        }

        public async Task<bool?> DeleteAsync(int id, bool saveChanges = true)
        {
            var entity = _dbContext.Set<T>().SingleOrDefault(e => e.Id == id);
            _dbContext.Set<T>().Remove(entity);
            if (saveChanges)
            {
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }

        public async Task<List<T>> ListAsync(ISpecification<T> spec)
        {
            if (spec == null)
                return await ListAllAsync();

            // fetch a Queryable that includes all expression-based includes
            var queryableResultWithIncludes = spec.Includes
                .Aggregate(_dbContext.Set<T>().AsQueryable(),
                    (current, include) => current.Include(include));

            // modify the IQueryable to include any string-based include statements
            var secondaryResult = spec.IncludeStrings
                .Aggregate(queryableResultWithIncludes,
                    (current, include) => current.Include(include));

            // return the result of the query using the specification's criteria expression
            return await secondaryResult
                            .Where(spec.Criteria)
                            .ToListAsync();
        }
    }
}
