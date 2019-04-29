using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BucketList<TValue>
{
	private readonly Dictionary<int, List<TValue>> collection;

	public BucketList(){
		collection = new Dictionary<int, List<TValue>>();
	}

	public void Add(TValue value, int bucket)
	{
		if (!collection.ContainsKey(bucket)) throw new System.ArgumentOutOfRangeException("Bucket doesn't exist");
		collection[bucket].Add(value);
	}

	public void CreateBucket(int bucket)
	{
		if (collection.ContainsKey(bucket)) throw new System.InvalidOperationException("Bucket already exist");
		collection.Add(bucket, new List<TValue>());
	}

	public void RemoveBucket(int bucket)
	{
		if (!collection.ContainsKey(bucket)) throw new System.InvalidOperationException("Bucket doesn't exist");
		collection.Remove(bucket);
	}

	public bool Exists(int bucket)
	{
		return collection.ContainsKey(bucket);
	}

	public IEnumerator<TValue> GetEnumerator(int bucket)
	{
		if(!collection.ContainsKey(bucket)) throw new System.InvalidOperationException("Bucket doesn't exist");
		return collection[bucket].GetEnumerator();
	}
}
