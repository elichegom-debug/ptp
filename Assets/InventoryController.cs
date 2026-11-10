using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : MonoBehaviour
{
    PanelRenderer renderer;         //인스펙터에서 볼 수 있는 애
    VisualElement slotContainer;    //ui툴킷 창을 열어야 보이는 애들
    Label itemInfoLabel;            //ui툴킷 창을 열어야 보이는 애들

    //인벤토리 구조화
    int totalSlotCount = 12;    //인벤토리 총 칸수
    public List<InventoryItem> initItems = new List<InventoryItem>();  //초기 아이템
    //리스트에서 자료의 수를 세는 방법 List.Count
    InventoryItem[] slots;      //아이템 칸
    bool isInit = false;        //초기화가 된 인벤토리인지를 확인, 씬변환이나 세이브 로드시에 보존할것

    VisualElement root;         //드랍포인트 보정용
    VisualElement dragGhostIcon;//고스트아이콘
    bool isDragging = false;    //클릭 이후 떼기 전까지 구별용
    int dragStartIndex = -1;    //클릭된 아이템의 번호저장
    int dragEndIndex = -1;      //내려놓을 아이템의 번호저장

    //슬롯 초기화 메서드
    void InitSlotData()
    {
        if(isInit == true)
        {
            return;
        }
        slots = new InventoryItem[totalSlotCount];  //인벤토리 총 칸수 구현
        for(int i=0; i < totalSlotCount; i++)
        {
            if(i < initItems.Count && initItems[i] != null)
            {
                //초기지급아이템을 빈 인벤토리에 저장
                slots[i] = initItems[i].Clone();    //주소참조로인한 공멸방지
            }
            else
            {
                slots[i] = new InventoryItem(); //빈칸을 저장
            }
            //Debug.Log(slots[i].name);
        }
        isInit = true;
    }
    
    void Start()
    {
        InitSlotData();
        renderer = GetComponent<PanelRenderer>();
        renderer.RegisterUIReloadCallback(OnUIReload);
        //Renderer에 아래 만든 메서드를 주입, UI그릴 때 실행해줌
    }

    void OnRootPointUp(PointerUpEvent e)
    {
        if(isDragging == false)
        {
            return; //마우스가 떼진상태에서는 아무것도 하지 않지만
        }
        isDragging = false; //마우스가 클릭된채라면 false로 변경해둔다
        if(dragGhostIcon != null)
        {
            dragGhostIcon.style.display = DisplayStyle.None;
        }
        if(dragEndIndex >= 0 || dragEndIndex < totalSlotCount && dragEndIndex != dragStartIndex)
        {
            //아이템 교체 또는 합성 기능 추가 부분
        }
        dragStartIndex = -1;
    }
    void HandleItemDrop(int fromIndex, int toIndex)
    {
        InventoryItem fromItem = slots[fromIndex];
        InventoryItem toItem = slots[toIndex];
        if (toItem.IsEmpty() == true)   //목적지에 아이템이 빈칸인 경우
        {
            slots[toIndex] = fromItem.Clone();  //복사
            slots[fromIndex].Clear();           //원래꺼 지움
            itemInfoLabel.text = "[" + slots[toIndex].name +"] 아이템을 "+(toIndex+1)+"번 슬롯으로 이동!!";
        }else if (fromItem.IsSameItem(toItem) == true && toItem.maxStack > 1)
        {
            //이름이 똑같고 소모품인것을 드래그해서 합친 상황
            int totalAmount = toItem.amount + fromItem.amount;
            if(totalAmount <= toItem.maxStack)
            {
                toItem.amount = totalAmount;
                fromItem.Clear();
                itemInfoLabel.text = "[" + slots[toIndex].name + "] 수량을 합쳤습니다! (현재수량 : "+ toItem.amount + ")";
            }
            else
            {
                int remain = totalAmount - toItem.maxStack;
                toItem.amount = toItem.maxStack;
                fromItem.amount = remain;
                itemInfoLabel.text = "[" + slots[toIndex].name + "] 최대수량!! (" + toItem.amount + "개) 까지 합쳤습니다.";
            }
        }
        else
        {
            InventoryItem temp = slots[toIndex];
            slots[toIndex] = slots[fromIndex];
            slots[fromIndex] = temp;
            itemInfoLabel.text = "[" + slots[toIndex].name + "] 과 ["+ slots[fromIndex].name + "] 아이템의 위치를 교환했습니다.";
        }
        BuildInventorySlots();
    }


    //PanelRenderer로 부터 내부 Element를 추출하기 위해 반드시 갖춰야 하는 규격
    //void타입의 메서드여야 하고 매개변수로 PanelRenderer, VisualElement, int 세개의 값을 가져야 한다.
    //이름은 마음대로 지어도 된다.
    //1번 패러미터 PanelRenderer renderer : 함수를 실행시킨 Renderer가 해당 매개변수에 저장
    //2번 패러미터 VisualElement root : 함수를 실행시킨 Renderer가 가진 UXML파일의 root를 해당 매개변수에 저장
    //3번 패러미터 int version : Renderer가 수회 다른 UXML을 변경해가며 그려야 할 경우 비교의 대상이 될 고유번호 지정
    void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        this.root = root;
        slotContainer = root.Q<VisualElement>("SlotContainer");
        itemInfoLabel = root.Q<Label>("ItemInfoLabel");
        CreateDragGhostIcon();
        root.RegisterCallback<PointerMoveEvent>(OnRootPointerMove);
        root.RegisterCallback<PointerUpEvent>(OnRootPointUp);
        BuildInventorySlots();
    }
    //유령 아이콘 제작
    void CreateDragGhostIcon()
    {
        dragGhostIcon = new VisualElement();
        dragGhostIcon.name = "Ghost";

        dragGhostIcon.style.position = Position.Absolute;
        dragGhostIcon.style.width = 64;
        dragGhostIcon.style.height = 64;
        dragGhostIcon.style.opacity = 0.5f;
        //dragGhostIcon.style.backgroundColor = Color.red;

        dragGhostIcon.pickingMode = PickingMode.Ignore;
        dragGhostIcon.style.display = DisplayStyle.None;
        root.Add(dragGhostIcon);
    }
    void OnRootPointerMove(PointerMoveEvent e)
    {
        if(isDragging == false || dragGhostIcon == null)
        {
            return;
        }
        dragGhostIcon.style.left = e.position.x - 32;
        dragGhostIcon.style.top = e.position.y - 32;
    }
    void BuildInventorySlots()
    {
        if(slotContainer == null){return;}
        slotContainer.Clear();

        for (int i=0; i < totalSlotCount; i++)
        {
            VisualElement slotElement = new VisualElement();
            slotElement.name = "Slot_" + i;
            slotElement.AddToClassList("inventory-slot");
            slotElement.userData = i;
            slotElement.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
            slotElement.RegisterCallback<PointerUpEvent>(OnSlotPointUp);
            slotElement.RegisterCallback<PointerEnterEvent>(OnSlotPointerEnter);
            slotElement.RegisterCallback<PointerLeaveEvent>(OnSlotPointerLeave);
            InventoryItem currentItem = slots[i];   //6 아이템이 있고, 6칸 아이템이 없다
            if (currentItem.IsEmpty() == false)
            {
                VisualElement iconElement = new VisualElement();
                iconElement.AddToClassList("slot-icon");
                iconElement.pickingMode = PickingMode.Ignore;   //마우스 가로채기 금지
                if(currentItem.icon != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(currentItem.icon);
                }
                slotElement.Add(iconElement);

                Label currentLabel = new Label(""+currentItem.amount);
                currentLabel.AddToClassList("slot-label");
                currentLabel.pickingMode = PickingMode.Ignore;   //마우스 가로채기 금지
                slotElement.Add(currentLabel);
            }                                        
            slotContainer.Add(slotElement);
        }
    }//UI그림그리는 메서드 종료지점

    //마우스 포인터가 특정 슬롯에 들어간 경우이벤트
    void OnSlotPointerEnter(PointerEnterEvent e)
    {
        VisualElement slot = (VisualElement)e.currentTarget;
        if(slot != null && slot.userData != null)
        {
            dragEndIndex = (int)slot.userData;
        }
    }
    void OnSlotPointerLeave(PointerLeaveEvent e)
    {
        VisualElement slot = (VisualElement)e.currentTarget;
        if (slot != null && slot.userData != null)
        {
            int index = (int)slot.userData;
            if(dragEndIndex == index)
            {
                dragEndIndex = -1;
            }
        }
    }
    void OnSlotPointUp(PointerUpEvent e)
    {
        if(isDragging == false)
        {
            return;
        }
        isDragging = false;
        if(dragGhostIcon != null)
        {
            dragGhostIcon.style.display = DisplayStyle.None;
        }
        //아이템교체, 아이템 합침, 아이템 조합, 아이템 이동
        //개별적으로 조건 처리 필요
        if (dragEndIndex >= 0 && dragEndIndex <totalSlotCount && dragEndIndex != dragStartIndex)
        {
            HandleItemDrop(dragStartIndex, dragEndIndex);
        }
        dragStartIndex = -1;
    }
    void OnSlotPointerDown(PointerDownEvent e)//인벤토리 클릭이벤트
    {
        VisualElement clickedSlot = (VisualElement)e.currentTarget;
        
        if (clickedSlot == null || clickedSlot.userData == null)
        {
            return;
        }
        int slotIndex = (int)clickedSlot.userData;
        if(e.button == 0)
        {
            //왼쪽 마우스 눌림
            ShowItemInfo(slotIndex);
            //드래그 시작, 고스트 등장
            if (slots[slotIndex].IsEmpty() == false)
            {
                isDragging = true;
                dragStartIndex = slotIndex;
                dragGhostIcon.style.backgroundImage = new StyleBackground(slots[slotIndex].icon);
                dragGhostIcon.style.display = DisplayStyle.Flex;
                dragGhostIcon.style.left = e.position.x - 32;
                dragGhostIcon.style.top = e.position.y - 32;
            }
        }
        else if(e.button == 1)
        {
            //마우스 오른쪽 눌림, 아이템 사용기능
            UseItem(slotIndex);
        }
    }
    void ShowItemInfo(int index)
    {
        InventoryItem clickedItem = slots[index];
        if(clickedItem.IsEmpty() == true)
        {
            itemInfoLabel.text = (index + 1) + "번 슬롯은 비어있습니다.";
        }
        else
        {
            itemInfoLabel.text = "[" + clickedItem.name + "] " + clickedItem.description;
        }
    }
    void UseItem(int index)
    {
        InventoryItem item = slots[index];
        if(item.IsEmpty() == true)
        {
            itemInfoLabel.text = (index + 1) + "번 슬롯에는 사용할 아이템이 없습니다.";
            return;
        }
        if(item.maxStack <= 1)
        {
            itemInfoLabel.text = "["+item.name+"] 아이템은 소모품이 아닙니다.";
            return;
        }
        string usedItem = item.name;
        item.amount -= 1;
        if(item.amount <= 0)
        {
            item.Clear();   //아이템 소멸
            itemInfoLabel.text = "[" + usedItem + "] 아이템을 모두 소모하였습니다.";
        }
        else
        {
            itemInfoLabel.text = "[" + usedItem + "] 아이템 사용! 남은 수량( "+ item.amount + "개 )";
        }
        BuildInventorySlots();
    }
}
[Serializable]
public class InventoryItem
{
    public string name; //물약
    public string description; //다쳤을때 사용 HP50회복
    public Sprite icon; //물약 모양
    public int amount;  //수량 현재 5개
    public int maxStack;//최대 겹치기 갯수 총 20개

    public InventoryItem()
    {
        Clear();
    }
    public InventoryItem(string a, string b, Sprite c, int d, int e)
    {
        name = a;
        description = b;
        icon = c;
        amount = d;
        maxStack = e;
    }
    public void Clear()
    {
        name = "";
        description = "";
        icon = null;
        amount = 0;
        maxStack = 0;
    }
    public InventoryItem Clone()
    {
        return new InventoryItem(name, description, icon, amount, maxStack);
    }
    public bool IsEmpty()   //지금 클릭된 좌표의 데이터가 빈칸인지 아닌지 판별
    {
        if(name == null || name == "" || amount == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool IsSameItem(InventoryItem other)
    {
        if(other == null)
        {
            return false;
        }
        if(this.IsEmpty() == true || other.IsEmpty() == true)
        {
            return false;
        }
        if(this.name == other.name)
        {
            return true;
        }
        else
        {
            return false;   //다른 경우의 예외가 발생할때 변경할것
        }
    }
}
