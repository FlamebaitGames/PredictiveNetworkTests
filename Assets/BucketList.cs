﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BucketList
{
	private readonly Dictionary<int, BucketInfo> collection;
	private struct BucketInfo
	{
		public List<PlayerInput> input;
		public PlayerState[] context;
	}
	public struct Bucket
	{
		public int frame;
		public List<PlayerInput> input;
		public PlayerState[] context;
	}

	public BucketList(){
		collection = new Dictionary<int, BucketInfo>();
	}

	public void Add(PlayerInput value, int bucket)
	{
		if (!collection.ContainsKey(bucket)) throw new System.ArgumentOutOfRangeException("Bucket doesn't exist");
		collection[bucket].input.Add(value);
	}

	public void CreateBucket(int bucket, PlayerState[] context)
	{
		if (collection.ContainsKey(bucket)) throw new System.InvalidOperationException("Bucket already exist");
		collection.Add(bucket, new BucketInfo
		{
			context = context.Select(c => new PlayerState
			{
				position = c.position,
				angularVelocity = c.angularVelocity,
				rotation = c.rotation,
				velocity = c.velocity,
				entityId = c.entityId,
				frame = bucket
			}).ToArray(),
			input = new List<PlayerInput>()
		});
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

	public PlayerState[] GetContext(int bucket)
	{
		if (!collection.ContainsKey(bucket)) throw new System.InvalidOperationException("Bucket doesn't exist");
		return collection[bucket].context;
	}

	public IEnumerable<PlayerInput> GetInputEnumerator(int bucket)
	{
		if(!collection.ContainsKey(bucket)) throw new System.InvalidOperationException("Bucket doesn't exist");
		return collection[bucket].input.AsEnumerable();
	}

	public void Trim(Predicate<Bucket> predicate)
	{
		var keys = collection.Keys.ToArray();
		foreach (var key in keys)
		{
			var col = collection[key];
			if (predicate(new Bucket
			{
				frame = key,
				context = col.context,
				input = col.input
			})) collection.Remove(key);
		}
		//Debug.Log($"Bucket Size: {collection.Count}");
	}
}
