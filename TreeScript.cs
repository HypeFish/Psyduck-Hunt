using System;
using System.Collections.Generic;
using UnityEngine;

public class TreeScript : MonoBehaviour
{
    public GameObject fracture;
    private GameObject pokeball;
    
    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag($"Pokeball") && pokeball == null && tag.Equals($"Tree"))
        {
            pokeball = collision.gameObject;
            GameObject fractureTree = Instantiate(fracture, transform.position, Quaternion.identity);
            Rigidbody[] fragments = fractureTree.GetComponentsInChildren<Rigidbody>();
    
            foreach (var rigid in fragments)
            {
                rigid.AddExplosionForce(500, fractureTree.transform.position, 10);
                Destroy(fractureTree, 10f);
            }
            
            Destroy(gameObject);
        }
    }
}