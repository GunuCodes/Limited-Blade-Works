using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
 
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
 
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;
 
    private Queue<DialogueLine> lines;
    
    public bool isDialogueActive = false;
 
    public float typingSpeed = 0.2f;
 
    public Animator animator;
 
    private bool isTyping = false;  // Track if text is currently being typed
 
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
 
        lines = new Queue<DialogueLine>();
    }
 
    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;
 
        animator.Play("dialogue_in");
 
        lines.Clear();
 
        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }
 
        DisplayNextDialogueLine();
        // Instead of disabling the entire PlayerController, we'll let it handle the input blocking
    }
 
    public void DisplayNextDialogueLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }
 
        DialogueLine currentLine = lines.Dequeue();

        characterName.text = currentLine.character.name;
 
        StopAllCoroutines();
 
        StartCoroutine(TypeSentence(currentLine));
    }
 
    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        isTyping = true;
        dialogueArea.text = "";
        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }
 
    void EndDialogue()
    {
        isDialogueActive = false;
        animator.Play("dialogue_out");
        // Remove the line that re-enables PlayerController since we're not disabling it anymore
    }
 
    public void Update()
    {
        // Only check for input if dialogue is active
        if (isDialogueActive)
        {
            // If currently typing and player clicks, complete the current text immediately
            if (isTyping && Input.GetMouseButtonDown(0))
            {
                StopAllCoroutines();
                DialogueLine currentLine = lines.Peek();  // Peek instead of dequeue to get current line
                dialogueArea.text = currentLine.line;  // Show full text immediately
                isTyping = false;
            }
            // If not typing and player clicks, move to next line
            else if (!isTyping && Input.GetMouseButtonDown(0))
            {
                DisplayNextDialogueLine();
            }
        }
    }
}