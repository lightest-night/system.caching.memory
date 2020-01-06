# Lightest Night - TIM TIM TIM
## Caching > Memory

Hooks, utilities and helpers to allow caching to a memory cache

#### How To Use
##### Registration
* Asp.Net Standard/Core Dependency Injection
  * Use the provided `services.AddMemoryCache()` method
  
* Other Containers
  * Register an instance of `ICache` as a Singleton forwarded to the `Cache` concrete type.

##### Usage
* `Task Save<TItem>(object key, TItem objectToSave, DateTime? expires, params string[] tags)`
  * An asynchronous function to call when saving an item to the memory cache
  * **NB** expiry is optional, if not provided, cached items will not expire
  
* `Task<TItem> Get(object key)`
  * An asynchronous function to call when retrieving an item from the memory cache
  
* `Task<IEnumerable<TItem>> GetByTag<TItem>(string tag)`
  * An asynchronous function to call when retrieving items by tag from the memory cache
  
* `Task Delete<TItem>(object key)`
  * An asynchronous function to call when deleting an item from the cache