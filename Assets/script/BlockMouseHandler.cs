using UnityEngine;

// Child block parcalarindan mouse event'lerini DraggableBlock'a ileten helper class
public class BlockMouseHandler : MonoBehaviour
{
    public DraggableBlock draggableBlock;
    
    void OnMouseDown()
    {
        if (draggableBlock != null)
        {
            draggableBlock.HandleMouseDown();
        }
    }
    
    void OnMouseDrag()
    {
        if (draggableBlock != null)
        {
            draggableBlock.HandleMouseDrag();
        }
    }
    
    void OnMouseUp()
    {
        if (draggableBlock != null)
        {
            draggableBlock.HandleMouseUp();
        }
    }
}
